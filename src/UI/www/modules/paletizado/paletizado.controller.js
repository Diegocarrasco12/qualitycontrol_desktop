if (!window.PaletizadoController) {

    class PaletizadoController {

        constructor() {
            this.paginaActual = 1
            this.registrosPorPagina = 20
            this.palets = []
            this.cargando = false
            this._eventsBound = false
            this._clickHandler = null
            this.exportando = false
        }

        async init() {
            this.bindEvents()
            this.iniciarCalendarios()
            await this.cargarPalets()
        }

        bindEvents() {

            if (this._eventsBound) return
            this._eventsBound = true

            this._clickHandler = (e) => {

                if (!document.getElementById("tablaPalets")) return

                if (e.target.id === "btnFiltrar") {
                    this.aplicarFiltros()
                }

                if (e.target.id === "btnLimpiar") {
                    this.limpiarFiltros()
                }

                if (e.target.id === "btnActualizar") {
                    this.cargarPalets()
                }

                if (e.target.id === "btnExportar") {
                    this.exportarExcel()
                }
            }

            document.addEventListener("click", this._clickHandler)
        }

        // =========================
        // DATA
        // =========================

        async cargarPalets() {

            if (this.cargando) return
            this.cargando = true

            try {

                const response = await window.PhotinoBridge.send({
                    action: "paletizado.obtenerPalets",
                    data: {
                        page: this.paginaActual,
                        limit: this.registrosPorPagina,
                        fechaDesde: document.getElementById("filtroFechaDesde")?.value || "",
                        fechaHasta: document.getElementById("filtroFechaHasta")?.value || "",
                        planta: document.getElementById("filtroPlanta")?.value || "",
                        taller: document.getElementById("filtroTaller")?.value || "",
                        np: document.getElementById("filtroNP")?.value || "",
                        idPalet: document.getElementById("filtroIdPalet")?.value || ""
                    }
                })

                if (!response || response.ok === false) {
                    throw new Error(response?.error || "Error backend")
                }

                const payload = response.data || {}

                this.palets = payload.items || []

                this.renderTabla(this.palets)
                this.renderPaginacion(payload.pages || 1)

            } catch (err) {
                console.error("❌ cargarPalets:", err)
            }

            this.cargando = false
        }

        // =========================
        // TABLA
        // =========================

        renderTabla(data) {

            const tbody = document.getElementById("tablaPalets")
            if (!tbody) return

            tbody.innerHTML = ""

            if (!data.length) {
                tbody.innerHTML = `<tr><td colspan="15">Sin registros</td></tr>`
                return
            }

            data.forEach(row => {

                const tr = document.createElement("tr")

                tr.innerHTML = `
    <td>${row.IdPalet ?? ""}</td>
    <td>${this.formatearFecha(row.Fecha)}</td>
    <td>${row.Planta ?? ""}</td>
    <td>${row.NPCliente ?? ""}</td>
    <td>${row.NPInnpack ?? ""}</td>
    <td>${row.NombreCliente ?? ""}</td>
    <td>${row.Taller ?? ""}</td>
    <td>${row.Tipo ?? ""}</td>
    <td>${row.Cantidad ?? ""}</td>
    <td>${row.UnidadesPorPliego ?? ""}</td>
    <td>$${this.formatMoney(row.ValorUnitario)}</td>
    <td>$${this.formatMoney(row.ValorTotal)}</td>
    <td>${row.Descripcion ?? ""}</td>
    <td>${this.formatearFechaSolo(row.FechaImpresion)}</td>
    <td>${row.EmisorTarja ?? ""}</td>
`

                tbody.appendChild(tr)
            })
        }

        // =========================
        // PAGINACIÓN
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

            if (this.paginaActual > 1) {
                const prev = document.createElement("button")
                prev.textContent = "←"
                prev.onclick = async () => {
                    this.paginaActual--
                    await this.cargarPalets()
                }
                container.appendChild(prev)
            }

            if (start > 1) {
                const dots = document.createElement("span")
                dots.textContent = "..."
                container.appendChild(dots)
            }

            for (let i = start; i <= end; i++) {

                const btn = document.createElement("button")
                btn.textContent = i

                if (i === this.paginaActual) {
                    btn.classList.add("active")
                }

                btn.onclick = async () => {
                    if (this.cargando) return
                    this.paginaActual = i
                    await this.cargarPalets()
                }

                container.appendChild(btn)
            }

            if (end < total) {
                const dots = document.createElement("span")
                dots.textContent = "..."
                container.appendChild(dots)
            }

            if (this.paginaActual < total) {
                const next = document.createElement("button")
                next.textContent = "→"
                next.onclick = async () => {
                    this.paginaActual++
                    await this.cargarPalets()
                }
                container.appendChild(next)
            }
        }

        // =========================
        // EXPORTAR
        // =========================

        async exportarExcel() {

            if (this.exportando) return
            this.exportando = true

            try {

                const res = await window.PhotinoBridge.send({
                    action: "paletizado.exportarExcel",
                    data: {
                        fechaDesde: document.getElementById("filtroFechaDesde")?.value || "",
                        fechaHasta: document.getElementById("filtroFechaHasta")?.value || "",
                        planta: document.getElementById("filtroPlanta")?.value || "",
                        taller: document.getElementById("filtroTaller")?.value || "",
                        np: document.getElementById("filtroNP")?.value || "",
                        idPalet: document.getElementById("filtroIdPalet")?.value || ""
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
            this.cargarPalets()
        }

        limpiarFiltros() {

            [
                "filtroFechaDesde",
                "filtroFechaHasta",
                "filtroPlanta",
                "filtroTaller",
                "filtroNP",
                "filtroIdPalet"
            ].forEach(id => {
                const el = document.getElementById(id)
                if (el) el.value = ""
            })

            this.cargarPalets()
        }

        formatearFecha(f) {
            if (!f) return ""
            const d = new Date(f)
            return isNaN(d) ? f : d.toLocaleString()
        }
        formatearFechaSolo(f) {
            if (!f) return ""

            // 🔥 cortar hora directamente
            if (typeof f === "string") {
                return f.split(" ")[0].split("-").reverse().join("/")
            }

            return ""
        }

        formatMoney(val) {
            if (val === null || val === undefined) return "0.00"

            return Number(val).toLocaleString("en-US", {
                minimumFractionDigits: 2,
                maximumFractionDigits: 2
            })
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

        destroy() {
            console.log("🧹 Destroy PaletizadoController")
        
            if (this._clickHandler) {
                document.removeEventListener("click", this._clickHandler)
            }
        
            this._clickHandler = null
            this._eventsBound = false
            this.cargando = false
            this.exportando = false
        }
    }

    window.PaletizadoController = PaletizadoController
}