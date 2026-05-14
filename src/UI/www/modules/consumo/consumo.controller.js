if (!window.ConsumoController) {

    // ==============================
    // CONSUMO CONTROLLER (FINAL ESTABLE)
    // ==============================

    class ConsumoController {

        constructor() {
            this.usuarioEditando = false
            this.cambiosPendientes = {}
            this.paginaActual = 1
            this.registrosPorPagina = 20
            this.consumos = []
            this.cargando = false
            this.autoRefreshTimer = null
            this._eventsBound = false

            this.exportando = false
        }

        async init() {
            this.bindEvents()
            this.iniciarCalendarios()

            await this.cargarConsumos()
            await this.cargarKpis()

            this.iniciarAutoRefresh()
        }

        bindEvents() {

            if (this._eventsBound) return
            this._eventsBound = true

            if (!this._clickHandler) {

                this._clickHandler = (e) => {

                    if (e.target.id === "btnFiltrar") {
                        this.aplicarFiltros()
                    }

                    if (e.target.id === "btnLimpiar") {
                        this.limpiarFiltros()
                    }

                    if (e.target.id === "btnGuardar") {
                        this.guardarCambios()
                    }

                    if (e.target.id === "btnExportar") {
                        this.exportarExcel()
                    }

                    if (e.target.id === "btnActualizar") {
                        this.cargarConsumos()
                    }
                }

                document.addEventListener("click", this._clickHandler)
            }
        }

        // =========================
        // DATA
        // =========================

        async cargarConsumos() {

            if (this.cargando) return
            this.cargando = true

            try {

                const response = await window.PhotinoBridge.send({
                    action: "consumo.obtenerConsumos",
                    data: {
                        page: this.paginaActual,
                        limit: this.registrosPorPagina,
                        fechaDesde: document.getElementById("filtroFechaDesde")?.value || "",
                        fechaHasta: document.getElementById("filtroFechaHasta")?.value || "",
                        codigo: document.getElementById("filtroCodigo")?.value || "",
                        lote: document.getElementById("filtroLote")?.value || ""
                    }
                })

                if (!response || response.ok === false) {
                    throw new Error(response?.error || "Error backend")
                }

                const payload = response.data || {}
                const items = Array.isArray(payload.items) ? payload.items : []

                this.consumos = items
                this.renderTabla(this.consumos)
                this.renderPaginacion(payload.pages || 1)

            } catch (err) {
                console.error("❌ cargarConsumos:", err)
            }

            this.cargando = false
        }

        async cargarKpis() {
            try {
                const res = await window.PhotinoBridge.send({
                    action: "consumo.obtenerKpis"
                })

                if (!res || res.ok === false) return

                const d = res.data || {}

                this.setText("kpiConsumoHoy", `${Number(d.consumoHoy || 0).toFixed(2)} kg`)
                this.setText("kpiTarjasHoy", Number(d.tarjasHoy || 0))
                this.setText("kpiSaldoTotal", `${Number(d.saldoTotal || 0).toFixed(2)} kg`)
                this.setText("kpiUltimoRegistro", `${d.ultimoCodigo ?? "--"} (${d.ultimoLote ?? "--"})`)

            } catch (err) {
                console.error("❌ KPI:", err)
            }
        }

        // =========================
        // TABLA
        // =========================

        renderTabla(data) {

            const tbody = document.getElementById("tablaConsumos")
            if (!tbody) return

            tbody.innerHTML = ""

            if (!data.length) {
                tbody.innerHTML = `<tr><td colspan="12">Sin registros</td></tr>`
                return
            }

            data.forEach(row => {

                const tr = document.createElement("tr")

                tr.innerHTML = `
                    <td>${row.Id ?? ""}</td>
                    <td>${this.formatearFecha(row.Fecha)}</td>
                    <td>${row.Descripcion ?? ""}</td>
                    <td>${row.Codigo ?? ""}</td>
                    <td>${row.ConsumoKg ?? ""}</td>
                    <td>${row.NP ?? ""}</td>
                    <td>${row.TarjaKg ?? ""}</td>
                    <td>${row.SaldoKg ?? ""}</td>
                    <td>${row.Lote ?? ""}</td>
                    <td>${row.UbicacionBin ?? row.ubicacion_bin ?? ""}</td>
    
                    <td>
                        <input value="${row.Estado ?? ""}" 
                            data-id="${row.Id}" 
                            data-field="estado">
                    </td>
    
                    <td>
                        <input value="${row.Salida ?? ""}" 
                            data-id="${row.Id}" 
                            data-field="salida">
                    </td>
                `

                tr.querySelectorAll("input").forEach(input => {

                    input.addEventListener("focus", () => {
                        this.usuarioEditando = true
                    })

                    input.addEventListener("input", (e) => {

                        const rowOriginal = this.consumos.find(r => r.Id == e.target.dataset.id)

                        this.registrarCambio(
                            e.target.dataset.id,
                            e.target.dataset.field,
                            e.target.value,
                            rowOriginal
                        )
                    })

                    input.addEventListener("paste", (e) => this.pegarVertical(e, input))
                })

                tbody.appendChild(tr)
            })
        }

        pegarVertical(event, input) {

            event.preventDefault()

            const valores = event.clipboardData.getData("text")
                .split(/\r?\n/)
                .filter(v => v.trim())

            const inputs = [...document.querySelectorAll(`input[data-field="${input.dataset.field}"]`)]
            const start = inputs.indexOf(input)

            valores.forEach((v, i) => {

                const target = inputs[start + i]
                if (!target) return

                target.value = v

                const row = this.consumos.find(r => r.Id == target.dataset.id)

                this.registrarCambio(
                    target.dataset.id,
                    target.dataset.field,
                    v,
                    row
                )
            })
        }

        // =========================
        // PAGINACIÓN (FIX CRÍTICO)
        // =========================

        renderPaginacion(total) {

            const container = document.getElementById("pagination")
            if (!container) return

            container.innerHTML = ""

            total = Number(total || 1)
            if (total <= 1) return

            const maxVisible = 5
            let start = Math.max(1, this.paginaActual - 2)
            let end = Math.min(total, start + maxVisible - 1)

            if (end - start < maxVisible - 1) {
                start = Math.max(1, end - maxVisible + 1)
            }

            // ← anterior
            if (this.paginaActual > 1) {
                const prev = document.createElement("button")
                prev.textContent = "←"
                prev.onclick = async () => {
                    this.paginaActual--
                    await this.cargarConsumos()
                }
                container.appendChild(prev)
            }

            // ...
            if (start > 1) {
                const dots = document.createElement("span")
                dots.textContent = "..."
                container.appendChild(dots)
            }

            // páginas visibles
            for (let i = start; i <= end; i++) {

                const btn = document.createElement("button")
                btn.textContent = i

                if (i === this.paginaActual) {
                    btn.classList.add("active")
                }

                btn.onclick = async () => {
                    if (this.cargando) return
                    this.paginaActual = i
                    await this.cargarConsumos()
                }

                container.appendChild(btn)
            }

            // ...
            if (end < total) {
                const dots = document.createElement("span")
                dots.textContent = "..."
                container.appendChild(dots)
            }

            // → siguiente
            if (this.paginaActual < total) {
                const next = document.createElement("button")
                next.textContent = "→"
                next.onclick = async () => {
                    this.paginaActual++
                    await this.cargarConsumos()
                }
                container.appendChild(next)
            }
        }

        // =========================
        // GUARDAR
        // =========================

        async guardarCambios() {

            const cambios = Object.entries(this.cambiosPendientes)
                .map(([id, c]) => ({

                    id: Number(id),
                    estado: c.estado ?? "",
                    salida: c.salida ?? ""

                }))
                .filter(c => c.id > 0)

            if (!cambios.length) {
                return
            }

            console.log("📤 cambios:", cambios)

            try {

                const res = await window.PhotinoBridge.send({
                    action: "consumo.guardarCambios",
                    data: cambios
                })

                if (!res || res.ok === false) {
                    console.error("❌ Error backend:", res?.error)
                    alert("Error al guardar")
                    return
                }

                alert("✅ Cambios guardados correctamente")

                this.cambiosPendientes = {}

                await this.cargarConsumos()

            } catch (err) {
                console.error("❌ guardarCambios:", err)
            }
        }

        registrarCambio(id, campo, valor, rowOriginal) {

            if (!this.cambiosPendientes[id]) {
                this.cambiosPendientes[id] = {
                    estado: rowOriginal?.Estado ?? "",
                    salida: rowOriginal?.Salida ?? ""
                }
            }

            this.cambiosPendientes[id][campo] = valor
        }

        // =========================
        // EXPORTAR
        // =========================

        async exportarExcel() {

            if (this.exportando) {
                console.warn("⚠ Ya se está exportando...")
                return
            }

            this.exportando = true

            try {

                const res = await window.PhotinoBridge.send({
                    action: "consumo.exportarExcel",
                    data: {
                        fechaDesde: document.getElementById("filtroFechaDesde")?.value || "",
                        fechaHasta: document.getElementById("filtroFechaHasta")?.value || "",
                        codigo: document.getElementById("filtroCodigo")?.value || "",
                        lote: document.getElementById("filtroLote")?.value || ""
                    }
                })

                if (!res || res.ok === false) {
                    alert("Error al exportar")
                    return
                }

                alert("✅ Excel generado correctamente\n\n📂 Ubicación:\n" + res.data?.path)
            } catch (err) {
                console.error("❌ exportarExcel:", err)
            } finally {
                this.exportando = false
            }
        }

        // =========================
        // UTILS
        // =========================

        aplicarFiltros() {
            this.paginaActual = 1
            this.cargarConsumos()
        }

        limpiarFiltros() {
            ["filtroFechaDesde", "filtroFechaHasta", "filtroCodigo", "filtroLote"]
                .forEach(id => {
                    const el = document.getElementById(id)
                    if (el) el.value = ""
                })

            this.cargarConsumos()
        }

        formatearFecha(f) {
            if (!f) return ""
            const d = new Date(f)
            return isNaN(d) ? f : d.toLocaleString()
        }

        setText(id, val) {
            const el = document.getElementById(id)
            if (el) el.innerText = val
        }

        iniciarCalendarios() {
            if (window.flatpickr) {
                flatpickr("#filtroFechaDesde", {
                    altInput: true,
                    altFormat: "d/m/Y",
                    dateFormat: "Y-m-d",
                    allowInput: true
                })
        
                flatpickr("#filtroFechaHasta", {
                    altInput: true,
                    altFormat: "d/m/Y",
                    dateFormat: "Y-m-d",
                    allowInput: true
                })
            }
        }

        iniciarAutoRefresh() {
            this.autoRefreshTimer = setInterval(() => {
                if (!this.usuarioEditando && !this.cargando) {
                    this.cargarConsumos()
                    this.cargarKpis()
                }
            }, 10000)
        }
        destroy() {
            console.log("🧹 Destroy ConsumoController")

            if (this.autoRefreshTimer) {
                clearInterval(this.autoRefreshTimer)
                this.autoRefreshTimer = null
            }

            if (this._clickHandler) {
                document.removeEventListener("click", this._clickHandler)
            }

            this._eventsBound = false
            this._clickHandler = null
            this.usuarioEditando = false
            this.cambiosPendientes = {}
            this.cargando = false
        }
    }

    window.ConsumoController = ConsumoController
}