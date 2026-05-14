// ==============================
// BINS CONTROLLER (FINAL ESTABLE)
// ==============================

if (!window.BinsController) {

    class BinsController {

        constructor() {
            this.registros = []
            this.paginaActual = 1
            this.registrosPorPagina = 20
            this.pages = 1

            this.cambiosPendientes = {}

            this.usuarioEditando = false
            this.cargando = false
            this.autoRefreshTimer = null

            this._eventsBound = false

        }

        // =========================
        // INIT
        // =========================
        async init() {
            console.log("🚀 INIT BINS")

            this.initDatePickers()
            this.bindEvents()

            await this.cargarDatos()

            this.iniciarAutoRefresh()
        }

        // =========================
        // DATEPICKERS
        // =========================
        initDatePickers() {

            if (typeof flatpickr === "undefined") {
                console.warn("⚠ flatpickr no cargado")
                return
            }
        
            flatpickr("#fechaDesde", {
                altInput: true,
                altFormat: "d/m/Y",
                dateFormat: "Y-m-d",
                allowInput: true
            })
        
            flatpickr("#fechaHasta", {
                altInput: true,
                altFormat: "d/m/Y",
                dateFormat: "Y-m-d",
                allowInput: true
            })
        }

        // =========================
        // EVENTS (DELEGACIÓN 🔥)
        // =========================
        bindEvents() {

            if (this._eventsBound) return
            this._eventsBound = true
        
            if (!this._clickHandler) {
        
                this._clickHandler = (e) => {
        
                    if (e.target.id === "btnFiltrar") {
                        this.paginaActual = 1
                        this.cargarDatos()
                    }
        
                    if (e.target.id === "btnLimpiar") {
                        this.limpiarFiltros()
                        this.paginaActual = 1
                        this.cargarDatos()
                    }
        
                    if (e.target.id === "btnGuardar") {
                        this.guardarCambios()
                    }
        
                    if (e.target.id === "btnExportar") {
                        this.exportarExcel()
                    }
        
                    if (e.target.dataset.page) {
                        this.paginaActual = Number(e.target.dataset.page)
                        this.cargarDatos()
                    }
        
                    if (e.target.classList.contains("btn-ver")) {
                        const file = e.target.dataset.file
        
                        window.PhotinoBridge.send({
                            action: "bins.abrirArchivo",
                            path: file
                        })
                    }
                }
        
                document.addEventListener("click", this._clickHandler)
            }
        }

        // =========================
        // DATA
        // =========================
        async cargarDatos() {

            if (this.cargando) return
            this.cargando = true

            try {

                const payload = {
                    page: this.paginaActual,
                    limit: this.registrosPorPagina,
                    fechaDesde: this.getVal("fechaDesde"),
                    fechaHasta: this.getVal("fechaHasta"),
                    bin: this.getVal("filtroBin"),
                    calle: this.getVal("filtroCalle"),
                    tipo: this.getVal("filtroTipo"),
                    documento: this.getVal("filtroDocumento")
                }

                const res = await window.PhotinoBridge.send({
                    action: "bins.obtenerRegistros",
                    data: payload
                })

                if (!res || res.ok === false) {
                    throw new Error(res?.error || "Error backend bins")
                }

                const data = res.data || {}

                this.registros = data.items || []
                this.pages = data.pages || 1
                this.totalRegistros = data.total || 0

                this.renderTabla()
                this.renderPaginacion()
                this.renderKpis()
                await this.cargarKPI()

            } catch (err) {
                console.error("❌ BINS ERROR:", err)
                this.renderError(err.message)
            }

            this.cargando = false
        }
        async cargarKPI() {
            try {
                const res = await window.PhotinoBridge.send({
                    action: "bins.obtenerKPI"
                })

                if (!res || res.ok === false) {
                    throw new Error(res?.error || "Error KPI")
                }

                const data = res.data || {}

                document.getElementById("kpiEntradas").textContent = data.entradasHoy || 0
                document.getElementById("kpiSalidas").textContent = data.salidasHoy || 0

            } catch (err) {
                console.error("❌ KPI ERROR:", err)
            }
        }

        // =========================
        // TABLA
        // =========================
        renderTabla() {

            const tbody = document.getElementById("tbodyBins")
            if (!tbody) return

            if (!this.registros.length) {
                tbody.innerHTML = `<tr><td colspan="10">Sin datos</td></tr>`
                return
            }

            tbody.innerHTML = this.registros.map(r => `
                <tr>
                    <td>${r.id}</td>
                    <td>${this.formatFechaHora(r.fecha)}</td>
                    <td>${r.numeroBin}</td>

                    <td>
                        <input value="${r.documento || ""}" data-id="${r.id}" data-field="documento" />
                    </td>

                    <td>${r.proveedor || ""}</td>

                    <td>
                        <input value="${r.estadoBin || ""}" data-id="${r.id}" data-field="estadoBin" />
                    </td>

                    <td><strong>${r.binCodigo || ""}</strong></td>
                    <td>${r.calle || ""}</td>
                    <td>${r.tipo}</td>

                    <td>
                    ${r.archivo
                    ? `<button class="btn-ver" data-file="${r.archivo}">Ver</button>`
                    : "-"
                }
                    </td>
                </tr>
            `).join("")

            this.bindInputs()
        }

        // =========================
        // INPUTS
        // =========================
        bindInputs() {

            document.querySelectorAll("input[data-id]").forEach(input => {

                input.addEventListener("focus", () => {
                    this.usuarioEditando = true
                })

                input.addEventListener("input", (e) => {

                    const id = e.target.dataset.id
                    const field = e.target.dataset.field
                    const value = e.target.value

                    const rowOriginal = this.registros.find(r => r.id == id)

                    this.registrarCambio(id, field, value, rowOriginal)
                })

                input.addEventListener("paste", (e) => this.pegarVertical(e, input))
            })
        }

        registrarCambio(id, campo, valor, rowOriginal) {

            if (!this.cambiosPendientes[id]) {
                this.cambiosPendientes[id] = {
                    documento: rowOriginal?.documento ?? "",
                    estadoBin: rowOriginal?.estadoBin ?? ""
                }
            }

            this.cambiosPendientes[id][campo] = valor
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

                const row = this.registros.find(r => r.id == target.dataset.id)

                this.registrarCambio(
                    target.dataset.id,
                    target.dataset.field,
                    v,
                    row
                )
            })
        }

        // =========================
        // GUARDAR
        // =========================
        async guardarCambios() {

            const cambios = Object.entries(this.cambiosPendientes)
                .map(([id, c]) => ({
                    id: Number(id),
                    documento: c.documento ?? "",
                    estadoBin: c.estadoBin ?? ""
                }))
                .filter(c => c.id > 0)

            if (!cambios.length) {
                alert("No hay cambios")
                return
            }

            const res = await window.PhotinoBridge.send({
                action: "bins.guardarCambios",
                cambios: cambios
            })

            if (!res || res.ok === false) {
                alert(res?.error || "Error guardando")
                return
            }

            alert("✅ Cambios guardados")

            this.cambiosPendientes = {}
            this.usuarioEditando = false

            await this.cargarDatos()
        }
        // =========================
        // EXPORTAR EXCEL
        // =========================
        async exportarExcel() {

            if (this.exportando) return
            this.exportando = true
        
            try {
        
                const res = await window.PhotinoBridge.send({
                    action: "bins.exportarExcel",
                    fechaDesde: this.getVal("fechaDesde"),
                    fechaHasta: this.getVal("fechaHasta"),
                    bin: this.getVal("filtroBin"),
                    calle: this.getVal("filtroCalle"),
                    tipo: this.getVal("filtroTipo"),
                    documento: this.getVal("filtroDocumento")
                })
        
                if (!res || res.ok === false) {
                    throw new Error(res?.error || "Error exportando")
                }
        
                const path = res.data?.path
        
                if (path) {
                    alert("✅ Excel generado correctamente\n\n📂 Ubicación:\n" + path)
                } else {
                    alert("Archivo generado correctamente")
                }
        
            } catch (err) {
                console.error("❌ Export error:", err)
                alert("Error exportando")
            } finally {
                this.exportando = false
            }
        }

        // =========================
        // KPIs
        // =========================
        renderKpis() {

            document.getElementById("kpiTotal").textContent = this.totalRegistros

            const ultimo = this.registros[0]?.numeroBin || "-"
            document.getElementById("kpiUltimo").textContent = ultimo
        }

        // =========================
        // PAGINACIÓN
        // =========================
        renderPaginacion() {

            const container = document.getElementById("paginacion")
            if (!container) return

            const total = this.pages
            const actual = this.paginaActual

            let html = ""

            const rango = 2 // páginas alrededor

            const start = Math.max(1, actual - rango)
            const end = Math.min(total, actual + rango)

            // 🔥 PRIMERA
            if (start > 1) {
                html += `<button data-page="1">1</button>`
                if (start > 2) {
                    html += `<span class="dots">...</span>`
                }
            }

            // 🔥 RANGO CENTRAL
            for (let i = start; i <= end; i++) {
                html += `
                    <button 
                        class="${i === actual ? "active" : ""}" 
                        data-page="${i}">
                        ${i}
                    </button>
                `
            }

            // 🔥 ÚLTIMA
            if (end < total) {
                if (end < total - 1) {
                    html += `<span class="dots">...</span>`
                }
                html += `<button data-page="${total}">${total}</button>`
            }

            container.innerHTML = html
        }

        // =========================
        // AUTO REFRESH
        // =========================
        iniciarAutoRefresh() {

            this.autoRefreshTimer = setInterval(() => {
                if (!this.usuarioEditando && !this.cargando) {
                    this.cargarDatos()
                }
            }, 10000)
        }

        // =========================
        // HELPERS
        // =========================
        getVal(id) {
            return document.getElementById(id)?.value || ""
        }

        limpiarFiltros() {
            ["fechaDesde", "fechaHasta", "filtroBin", "filtroCalle", "filtroTipo", "filtroDocumento"]
                .forEach(id => {
                    const el = document.getElementById(id)
                    if (el) el.value = ""
                })
        }

        formatFechaHora(fecha) {
            return fecha || ""
        }
        getFileUrl(path) {
            if (!path) return ""

            return "../" + path.replace(/^\/+/, "")
        }

        renderError(msg) {
            const tbody = document.getElementById("tbodyBins")
            if (!tbody) return
            tbody.innerHTML = `<tr><td colspan="10">❌ ${msg}</td></tr>`
        }

        destroy() {
            console.log("🧹 Destroy BinsController")
        
            if (this.autoRefreshTimer) {
                clearInterval(this.autoRefreshTimer)
                this.autoRefreshTimer = null
            }
        
            if (this._clickHandler) {
                document.removeEventListener("click", this._clickHandler)
            }
        
            this._eventsBound = false
            this._clickHandler = null
            this.cambiosPendientes = {}
            this.usuarioEditando = false
            this.cargando = false
        }
    }

    window.BinsController = BinsController
}