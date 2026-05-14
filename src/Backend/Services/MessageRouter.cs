using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using LogisticControlCenter.Modules.Altillo;
using LogisticControlCenter.Modules.Auth;
using LogisticControlCenter.Modules.Bins;
using LogisticControlCenter.Modules.BinsLavado;
using LogisticControlCenter.Modules.ConsumoPapel;
using LogisticControlCenter.Modules.Home;
using LogisticControlCenter.Modules.Paletizado;
using LogisticControlCenter.Modules.Palets;
using LogisticControlCenter.Modules.Usuarios;

namespace LogisticControlCenter.Services
{
    public class MessageRouter
    {
        private static bool _exportEnCurso = false;
        private readonly DbService _db;
        private readonly AuthHandler _authHandler;
        private readonly CurrentUserSessionService _session;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public MessageRouter(
            DbService db,
            AuthHandler authHandler,
            CurrentUserSessionService session
        )
        {
            _db = db;
            _authHandler = authHandler;
            _session = session;
        }

        public async Task<string> Handle(string payloadJson)
        {
            var startTime = DateTime.Now;

            try
            {
                Log("INFO", $"📩 PAYLOAD: {payloadJson}");

                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson);

                if (data == null)
                    return Error("Payload inválido");

                if (!data.ContainsKey("action"))
                    return Error("Falta 'action'");

                var action = data["action"]?.ToString();
                // 🔥 BLOQUE GLOBAL DE EXPORT
                if (action != null && action.Contains("exportarExcel"))
                {
                    if (_exportEnCurso)
                    {
                        Log("ERROR", "⚠ Export ya en curso bloqueado");
                        return Error("Ya hay una exportación en proceso");
                    }

                    _exportEnCurso = true;
                }

                if (string.IsNullOrEmpty(action))
                    return Error("Acción vacía");

                Log("INFO", $"🎯 ACTION: {action}");

                string rawResult;

                if (action.StartsWith("auth"))
                {
                    if (!data.ContainsKey("data"))
                        return Error("Falta 'data'");

                    if (data["data"] is not JsonElement authDataElement)
                        return Error("Formato inválido en 'data'");

                    rawResult = await _authHandler.Handle(action, authDataElement);
                }
                else if (action.StartsWith("consumo"))
                {
                    var handler = new ConsumoPapelHandler(_db);
                    rawResult = await handler.Handle(action, data);
                }
                else if (action.StartsWith("altillo"))
                {
                    var handler = new AltilloHandler(_db);
                    rawResult = await handler.Handle(action, data);
                }
                else if (action.StartsWith("binsLavado"))
                {
                    var handler = new BinsLavadoHandler(_db);
                    rawResult = await handler.Handle(action, data);
                }
                else if (action.StartsWith("binsPrint"))
                {
                    var handler = new LogisticControlCenter.Modules.BinsPrint.BinsPrintHandler();
                    rawResult = await handler.Handle(action, data);
                }
                else if (action.StartsWith("bins"))
                {
                    var handler = new BinsHandler(_db);
                    rawResult = await handler.Handle(action, data);
                }
                else if (action.StartsWith("palets"))
                {
                    var handler = new PaletsHandler(_db);
                    rawResult = await handler.Handle(action, data);
                }
                else if (action.StartsWith("paletizado"))
                {
                    var handler = new PaletizadoHandler(_db);
                    rawResult = await handler.Handle(action, data);
                }
                else if (action.StartsWith("inicio"))
                {
                    var handler = new HomeHandler(_db);
                    rawResult = await handler.Handle(action, data);
                }
                else if (action.StartsWith("usuarios"))
                {
                    var handler = new UsuariosHandler(_db, _session);
                    rawResult = await handler.Handle(action, data);
                }
                else
                {
                    return Error($"Acción no reconocida: {action}");
                }

                var normalized = NormalizeResponse(rawResult);
                if (action != null && action.Contains("exportarExcel"))
                {
                    _exportEnCurso = false;
                }
                var duration = (DateTime.Now - startTime).TotalMilliseconds;
                Log("SUCCESS", $"⏱ {action} en {duration}ms");

                return normalized;
            }
            catch (Exception ex)
            {
                Log("ERROR", $"❌ ROUTER ERROR: {ex.Message}");

                // 🔥 liberar lock si era export
                if (payloadJson != null && payloadJson.Contains("exportarExcel"))
                {
                    _exportEnCurso = false;
                }

                return Error(ex.Message);
            }
        }

        private string NormalizeResponse(string raw)
        {
            try
            {
                if (string.IsNullOrEmpty(raw))
                {
                    return JsonSerializer.Serialize(
                        new
                        {
                            ok = true,
                            success = true,
                            data = (object?)null,
                            error = (string?)null,
                        },
                        _jsonOptions
                    );
                }

                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                // ✅ CASO NORMAL: ya viene con { ok: true/false }
                if (root.TryGetProperty("ok", out var okProp))
                {
                    var ok = okProp.GetBoolean();

                    return JsonSerializer.Serialize(
                        new
                        {
                            ok = ok,
                            success = ok, // 🔥 compatibilidad frontend
                            data = root.TryGetProperty("data", out var dataProp)
                                ? JsonSerializer.Deserialize<object>(dataProp.GetRawText())
                                : null,
                            error = root.TryGetProperty("error", out var errProp)
                                ? errProp.GetString()
                                : null,
                        },
                        _jsonOptions
                    );
                }

                // ✅ CASO: respuesta simple (sin estructura)
                return JsonSerializer.Serialize(
                    new
                    {
                        ok = true,
                        success = true,
                        data = JsonSerializer.Deserialize<object>(raw),
                        error = (string?)null,
                    },
                    _jsonOptions
                );
            }
            catch
            {
                // ✅ fallback extremo
                return JsonSerializer.Serialize(
                    new
                    {
                        ok = true,
                        success = true,
                        data = raw,
                        error = (string?)null,
                    },
                    _jsonOptions
                );
            }
        }

        private string Error(string message)
        {
            return JsonSerializer.Serialize(
                new
                {
                    ok = false,
                    success = false,
                    data = (object?)null,
                    error = message,
                },
                _jsonOptions
            );
        }

        private void Log(string type, string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");

            switch (type)
            {
                case "ERROR":
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case "SUCCESS":
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
            }

            Console.WriteLine($"[{timestamp}] [{type}] {message}");
            Console.ResetColor();
        }
    }
}
