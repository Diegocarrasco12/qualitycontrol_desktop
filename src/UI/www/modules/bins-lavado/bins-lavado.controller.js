// ==============================
// BINS LAVADO CONTROLLER (PRO FINAL)
// ==============================

if (!window.BinsLavadoController) {

    class BinsLavadoController {

        constructor() {
            this.data = []
            this.page = 1
            this.limit = 20
            this.pages = 1

            this.loading = false
            this._eventsBound = false 
        }

        async init() {
            console.log("🚀 INIT BINS LAVADO")
            this.initDatePickers()
            this.bindEvents()
            await this.cargar()
            await this.cargarKPI()
        }
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
        // EVENTS
        // =========================
        bindEvents() {

            if (this._eventsBound) return
            this._eventsBound = true

            if (!this._clickHandler) {

                this._clickHandler = (e) => {

                    if (e.target.id === "btnBuscar") {
                        this.page = 1
                        this.cargar()
                        this.cargarKPI()
                    }

                    if (e.target.id === "btnLimpiar") {
                        this.limpiarFiltros()
                        this.page = 1
                        this.cargar()
                        this.cargarKPI()
                    }

                    if (e.target.id === "btnExportar") {
                        this.exportarExcel()
                    }

                    if (e.target.dataset.page) {
                        this.page = Number(e.target.dataset.page)
                        this.cargar()
                    }
                }

                document.addEventListener("click", this._clickHandler)
            }
        }

        // =========================
        // DATA
        // =========================
        async cargar() {
            if (this.loading) return
            this.loading = true

            try {

                this.renderLoading()

                const payload = this.getFiltros()

                const res = await window.PhotinoBridge.send({
                    action: "binsLavado.obtener",
                    data: payload
                })

                if (!res || res.ok === false) {
                    throw new Error(res?.error || "Error backend")
                }

                this.data = res.data.items || []
                this.pages = res.data.pages || 1

                this.render()
                this.renderPaginacion()

            } catch (err) {
                console.error("❌ ERROR:", err)
                this.renderError(err.message)
            }

            this.loading = false
        }

        // =========================
        // KPI
        // =========================
        async cargarKPI() {
            try {
                const res = await window.PhotinoBridge.send({
                    action: "binsLavado.obtenerKPI"
                })

                if (!res || res.ok === false) return

                document.getElementById("kpiTotal").textContent = res.data.totalHoy || 0

            } catch (err) {
                console.error("❌ KPI ERROR:", err)
            }
        }

        // =========================
        // EXPORT EXCEL
        // =========================
        async exportarExcel() {
            try {
        
                const payload = this.getFiltros()
        
                const res = await window.PhotinoBridge.send({
                    action: "binsLavado.exportarExcel",
                    data: payload
                })
        
                if (!res || res.ok === false) {
                    throw new Error(res?.error || "Error exportando")
                }
        
                alert("Excel generado correctamente")
        
            } catch (err) {
                console.error("❌ EXPORT ERROR:", err)
                alert("Error exportando Excel")
            }
        }

        // =========================
        // TABLA
        // =========================
        render() {
            const tbody = document.getElementById("binsLavadoTableBody")
            if (!tbody) return

            if (!this.data.length) {
                tbody.innerHTML = `
                    <tr>
                        <td colspan="10" style="text-align:center;">Sin datos</td>
                    </tr>
                `
                return
            }

            tbody.innerHTML = this.data.map(item => `
                <tr>
                    <td>${item.id}</td>
                    <td>${this.formatFecha(item.fecha)}</td>
                    <td>${item.numeroBin}</td>
                    <td>${item.documento}</td>
                    <td>${item.proveedor}</td>
                    <td>${item.estadoBin}</td>
                    <td><strong>${item.binCodigo}</strong></td>
                    <td>${item.calle}</td>
                    <td>${item.tipo}</td>
                    <td>
                        ${item.archivo
                    ? `<button class="btn-primary btn-sm" data-path="${item.archivo}">Ver</button>`
                    : '-'}
                    </td>
                </tr>
            `).join("")

            // 🔥 delegación evento abrir archivo
            tbody.querySelectorAll("button[data-path]").forEach(btn => {
                btn.addEventListener("click", () => {
                    this.abrirArchivo(btn.dataset.path)
                })
            })
        }

        // =========================
        // ABRIR ARCHIVO (PRO)
        // =========================
        async abrirArchivo(path) {
            try {
                await window.PhotinoBridge.send({
                    action: "binsLavado.abrirArchivo",
                    path
                })
            } catch (err) {
                console.error("❌ ERROR abrir archivo:", err)
            }
        }

        // =========================
        // PAGINACIÓN
        // =========================
        renderPaginacion() {

            const container = document.getElementById("paginacion")
            if (!container) return

            let html = ""

            const rango = 2 // 👈 ajustable

            let inicio = Math.max(1, this.page - rango)
            let fin = Math.min(this.pages, this.page + rango)

            // 👉 Primera
            if (inicio > 1) {
                html += `<button data-page="1">1</button>`
                if (inicio > 2) html += `<span>...</span>`
            }

            // 👉 Rango central
            for (let i = inicio; i <= fin; i++) {
                html += `
                    <button 
                        class="${i === this.page ? "active" : ""}" 
                        data-page="${i}">
                        ${i}
                    </button>
                `
            }

            // 👉 Última
            if (fin < this.pages) {
                if (fin < this.pages - 1) html += `<span>...</span>`
                html += `<button data-page="${this.pages}">${this.pages}</button>`
            }

            container.innerHTML = html

            container.querySelectorAll("button").forEach(btn => {
                btn.addEventListener("click", (e) => {
                    this.page = Number(e.target.dataset.page)
                    this.cargar()
                })
            })
        }

        // =========================
        // HELPERS
        // =========================
        getFiltros() {
            return {
                page: this.page,
                limit: this.limit,
                fechaDesde: this.getVal("fechaDesde"),
                fechaHasta: this.getVal("fechaHasta"),
                bin: this.getVal("filtroBin"),
                calle: this.getVal("filtroCalle"),
                documento: this.getVal("filtroDocumento")
            }
        }

        getVal(id) {
            return document.getElementById(id)?.value || ""
        }

        limpiarFiltros() {
            ["fechaDesde", "fechaHasta", "filtroBin", "filtroCalle", "filtroDocumento"]
                .forEach(id => {
                    const el = document.getElementById(id)
                    if (el) el.value = ""
                })
        }

        formatFecha(fecha) {
            return fecha || ""
        }

        renderLoading() {
            const tbody = document.getElementById("binsLavadoTableBody")
            if (!tbody) return

            tbody.innerHTML = `
                <tr>
                    <td colspan="10" style="text-align:center;">Cargando...</td>
                </tr>
            `
        }

        renderError(msg) {
            const tbody = document.getElementById("binsLavadoTableBody")
            if (!tbody) return

            tbody.innerHTML = `
                <tr>
                    <td colspan="10" style="color:red; text-align:center;">
                        ERROR: ${msg}
                    </td>
                </tr>
            `
        }

        arrayBufferToBase64(buffer) {
            let binary = ''
            let bytes = new Uint8Array(buffer)
            let len = bytes.byteLength
            for (let i = 0; i < len; i++) {
                binary += String.fromCharCode(bytes[i])
            }
            return window.btoa(binary)
        }

        destroy() {
            console.log("🧹 Destroy BinsLavadoController")
        
            if (this._clickHandler) {
                document.removeEventListener("click", this._clickHandler)
            }
        
            this._eventsBound = false
            this._clickHandler = null
            this.loading = false
        }
    }

    window.BinsLavadoController = BinsLavadoController
}