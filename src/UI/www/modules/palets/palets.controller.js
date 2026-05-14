// ==============================
// PALETS CONTROLLER (ULTRA PRO)
// ==============================

if (!window.PaletsController) {

    class PaletsController {

        constructor() {
            this.data = []
            this.page = 1
            this.limit = 20
            this.pages = 1
            this.loading = false

            this._eventsBound = false
            this.exportando = false
        }

        async init() {
            console.log("🚀 INIT PALETS")

            this.initDatePickers()
            this.bindEvents()

            await this.cargarDatos()
        }

        // =========================
        // DATEPICKERS
        // =========================
        initDatePickers() {
            if (typeof flatpickr === "undefined") return
        
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
        // EVENTS
        // =========================
        bindEvents() {

            if (this._eventsBound) return
            this._eventsBound = true

            if (!this._clickHandler) {
                if (!document.getElementById("tbodyPalets")) return

                this._clickHandler = (e) => {

                    if (e.target.id === "btnBuscar") {
                        if (this.loading) return
                        this.page = 1
                        this.cargarDatos()
                    }

                    if (e.target.id === "btnLimpiar") {
                        this.limpiarFiltros()
                        this.page = 1
                        this.cargarDatos()
                    }

                    if (e.target.id === "btnExportar") {
                        this.exportarExcel()
                    }

                    if (e.target.dataset.page) {
                        this.page = Number(e.target.dataset.page)
                        this.cargarDatos()
                    }
                }

                document.addEventListener("click", this._clickHandler)
            }
        }

        // =========================
        // DATA
        // =========================
        async cargarDatos() {

            if (this.loading) return
            this.loading = true

            this.renderLoading()


            try {

                const payload = {
                    page: this.page,
                    limit: this.limit,
                    fechaDesde: this.getVal("fechaDesde"),
                    fechaHasta: this.getVal("fechaHasta"),
                    lote: this.getVal("filtroLote"),
                    planta: this.getVal("filtroPlanta"),
                    tipo: this.getVal("filtroTipo")
                }
                const res = await window.PhotinoBridge.send({
                    action: "palets.obtenerRegistros",
                    data: payload
                })

                if (!res || res.ok === false) {
                    throw new Error(res?.error || "Error backend palets")
                }
                this.totalRegistros = res.data.total || 0

                this.data = res.data.items || []
                this.pages = res.data.pages || 1

                this.render()
                this.renderPaginacion()

            } catch (err) {
                console.error("❌ PALETS ERROR:", err)
                this.renderError(err.message)
            }

            this.loading = false
            this.renderKPI()
        }
        renderKPI() {
            const kpi = document.getElementById("kpiTotal")
            if (kpi) {
                kpi.textContent = this.totalRegistros || 0
            }
        }

        // =========================
        // KPI
        // =========================
        async cargarKPI() {
            try {

                const res = await window.PhotinoBridge.send({
                    action: "palets.obtenerKPI"
                })

                if (!res || res.ok === false) return

                const hoy = res.data.totalHoy || 0
                const ultimo = res.data.totalUltimoDia || 0

                const kpi = document.getElementById("kpiTotal")

                // 🔥 lógica inteligente
                if (kpi) {
                    kpi.textContent = hoy > 0 ? hoy : ultimo
                }

            } catch (err) {
                console.warn("KPI error:", err)
            }
        }

        // =========================
        // EXPORT
        // =========================
        async exportarExcel() {

            if (this.exportando) return
            this.exportando = true

            try {

                const res = await window.PhotinoBridge.send({
                    action: "palets.exportarExcel",
                    data: {
                        fechaDesde: this.getVal("fechaDesde"),
                        fechaHasta: this.getVal("fechaHasta"),
                        lote: this.getVal("filtroLote"),
                        planta: this.getVal("filtroPlanta"),
                        tipo: this.getVal("filtroTipo")
                    }
                })

                if (!res || res.ok === false) {
                    throw new Error(res?.error || "Error exportando")
                }

                if (res.path) {
                    window.open(res.path, "_blank")
                } else {
                    alert("Archivo generado")
                }

            } catch (err) {
                console.error("❌ EXPORT ERROR:", err)
                alert("Error exportando")
            } finally {
                this.exportando = false
            }
        }

        // =========================
        // TABLA
        // =========================
        render() {

            const tbody = document.getElementById("tbodyPalets")
            if (!tbody) return

            if (!this.data.length) {
                tbody.innerHTML = `<tr><td colspan="8">Sin datos</td></tr>`
                return
            }

            tbody.innerHTML = this.data.map(r => `
                <tr>

                    <td>${r.id}</td>

                    <td>${this.formatFecha(r.fecha)}</td>

                    <td><strong>${r.lote}</strong></td>

                    <td>${r.ean13}</td>

                    <td style="color:#888;">${r.detalle || "-"}</td>

                    <td>${r.planta}</td>

                    <td>
                        <span class="${r.tipoMovimiento === 'ENTRADA' ? 'badge-success' : 'badge-danger'}">
                            ${r.tipoMovimiento}
                        </span>
                    </td>

                    <td style="text-align:right;">${r.cantidad}</td>

                </tr>
            `).join("")
        }

        renderLoading() {
            const tbody = document.getElementById("tbodyPalets")
            if (!tbody) return

            tbody.innerHTML = `<tr><td colspan="8">Cargando...</td></tr>`
        }

        // =========================
        // PAGINACIÓN
        // =========================
        renderPaginacion() {

            const container = document.getElementById("paginacion")
            if (!container) return

            let html = ""

            const rango = 2

            let inicio = Math.max(1, this.page - rango)
            let fin = Math.min(this.pages, this.page + rango)

            // 👈 anterior
            if (this.page > 1) {
                html += `<button data-page="${this.page - 1}">←</button>`
            }

            // 👈 primera
            if (inicio > 1) {
                html += `<button data-page="1">1</button>`
                if (inicio > 2) html += `<span>...</span>`
            }

            // 👈 rango central
            for (let i = inicio; i <= fin; i++) {
                html += `
        <button 
            class="${i === this.page ? "active" : ""}" 
            data-page="${i}">
            ${i}
        </button>
    `
            }

            // 👈 última
            if (fin < this.pages) {
                if (fin < this.pages - 1) html += `<span>...</span>`
                html += `<button data-page="${this.pages}">${this.pages}</button>`
            }

            // 👉 siguiente
            if (this.page < this.pages) {
                html += `<button data-page="${this.page + 1}">→</button>`
            }

            container.innerHTML = html


        }

        // =========================
        // HELPERS
        // =========================
        getVal(id) {
            return document.getElementById(id)?.value || ""
        }

        limpiarFiltros() {
            ["fechaDesde", "fechaHasta", "filtroLote", "filtroPlanta", "filtroTipo"]
                .forEach(id => {
                    const el = document.getElementById(id)
                    if (el) el.value = ""
                })
        }

        formatFecha(fecha) {
            return fecha || ""
        }

        renderError(msg) {
            const tbody = document.getElementById("tbodyPalets")
            if (!tbody) return

            tbody.innerHTML = `<tr><td colspan="8">❌ ${msg}</td></tr>`
        }

        destroy() {
            console.log("🧹 Destroy PaletsController")

            if (this._clickHandler) {
                document.removeEventListener("click", this._clickHandler)
            }

            this._clickHandler = null
            this._eventsBound = false
            this.loading = false
            this.exportando = false
        }
    }

    window.PaletsController = PaletsController
}