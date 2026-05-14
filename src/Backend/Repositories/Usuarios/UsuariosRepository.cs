using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogisticControlCenter.Models;
using LogisticControlCenter.Services;
using MySqlConnector;

namespace LogisticControlCenter.Repositories.Usuarios
{
    public class UsuariosRepository
    {
        private readonly DbService _db;

        public UsuariosRepository(DbService db)
        {
            _db = db;
        }

        public async Task<List<User>> GetAllAsync()
        {
            var usuarios = new List<User>();

            using var conn = _db.GetConsumoPapelConnection();
            await conn.OpenAsync();

            const string sql =
                @"
                SELECT
                    id,
                    codigo_usuario,
                    nombre_completo,
                    password_hash,
                    rol,
                    activo,
                    creado_en,
                    actualizado_en
                FROM usuarios_sistema
                ORDER BY id DESC;
            ";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                usuarios.Add(
                    new User
                    {
                        Id = reader.GetInt32("id"),
                        CodigoUsuario = reader.GetString("codigo_usuario"),
                        NombreCompleto = reader.GetString("nombre_completo"),
                        PasswordHash = reader.GetString("password_hash"),
                        Rol = reader.GetString("rol"),
                        Activo = reader.GetBoolean("activo"),
                        CreadoEn = reader.GetDateTime("creado_en"),
                        ActualizadoEn = reader.GetDateTime("actualizado_en"),
                    }
                );
            }

            return usuarios;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            using var conn = _db.GetConsumoPapelConnection();
            await conn.OpenAsync();

            const string sql =
                @"
                SELECT
                    id,
                    codigo_usuario,
                    nombre_completo,
                    password_hash,
                    rol,
                    activo,
                    creado_en,
                    actualizado_en
                FROM usuarios_sistema
                WHERE id = @id
                LIMIT 1;
            ";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new User
            {
                Id = reader.GetInt32("id"),
                CodigoUsuario = reader.GetString("codigo_usuario"),
                NombreCompleto = reader.GetString("nombre_completo"),
                PasswordHash = reader.GetString("password_hash"),
                Rol = reader.GetString("rol"),
                Activo = reader.GetBoolean("activo"),
                CreadoEn = reader.GetDateTime("creado_en"),
                ActualizadoEn = reader.GetDateTime("actualizado_en"),
            };
        }

        public async Task<User?> GetByCodigoUsuarioAsync(string codigoUsuario)
        {
            using var conn = _db.GetConsumoPapelConnection();
            await conn.OpenAsync();

            const string sql =
                @"
                SELECT
                    id,
                    codigo_usuario,
                    nombre_completo,
                    password_hash,
                    rol,
                    activo,
                    creado_en,
                    actualizado_en
                FROM usuarios_sistema
                WHERE codigo_usuario = @codigoUsuario
                LIMIT 1;
            ";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@codigoUsuario", codigoUsuario);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new User
            {
                Id = reader.GetInt32("id"),
                CodigoUsuario = reader.GetString("codigo_usuario"),
                NombreCompleto = reader.GetString("nombre_completo"),
                PasswordHash = reader.GetString("password_hash"),
                Rol = reader.GetString("rol"),
                Activo = reader.GetBoolean("activo"),
                CreadoEn = reader.GetDateTime("creado_en"),
                ActualizadoEn = reader.GetDateTime("actualizado_en"),
            };
        }

        public async Task<bool> ExistsByCodigoUsuarioAsync(string codigoUsuario)
        {
            using var conn = _db.GetConsumoPapelConnection();
            await conn.OpenAsync();

            const string sql =
                @"
                SELECT COUNT(*)
                FROM usuarios_sistema
                WHERE codigo_usuario = @codigoUsuario;
            ";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@codigoUsuario", codigoUsuario);

            var result = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return result > 0;
        }

        public async Task<int> CreateAsync(User user)
        {
            using var conn = _db.GetConsumoPapelConnection();
            await conn.OpenAsync();

            const string sql =
                @"
                INSERT INTO usuarios_sistema
                (
                    codigo_usuario,
                    nombre_completo,
                    password_hash,
                    rol,
                    activo,
                    creado_en,
                    actualizado_en
                )
                VALUES
                (
                    @codigoUsuario,
                    @nombreCompleto,
                    @passwordHash,
                    @rol,
                    @activo,
                    NOW(),
                    NOW()
                );

                SELECT LAST_INSERT_ID();
            ";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@codigoUsuario", user.CodigoUsuario);
            cmd.Parameters.AddWithValue("@nombreCompleto", user.NombreCompleto);
            cmd.Parameters.AddWithValue("@passwordHash", user.PasswordHash);
            cmd.Parameters.AddWithValue("@rol", user.Rol);
            cmd.Parameters.AddWithValue("@activo", user.Activo);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _db.GetConsumoPapelConnection();
            await conn.OpenAsync();

            const string sql =
                @"
                DELETE FROM usuarios_sistema
                WHERE id = @id;
            ";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> UpdatePasswordAsync(int id, string passwordHash)
        {
            using var conn = _db.GetConsumoPapelConnection();
            await conn.OpenAsync();

            const string sql =
                @"
        UPDATE usuarios_sistema
        SET
            password_hash = @passwordHash,
            actualizado_en = NOW()
        WHERE id = @id;
    ";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@passwordHash", passwordHash);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}
