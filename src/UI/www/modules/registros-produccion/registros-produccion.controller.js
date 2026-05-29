if (!window.RegistrosProduccionController) {
    class RegistrosProduccionController {
        constructor() {
            this.data = null
            this.loading = false
            this.filtro = ""
            this._clickHandler = null
            this._inputHandler = null
        }

        async init() {
            console.log("INIT REGISTROS PRODUCCION")
            this.bindEvents()
            this.initDatePickers()
            await this.cargarDatos()
        }

        initDatePickers() {
            if (!window.flatpickr) return

            flatpickr("#fechaDesdeProduccion", {
                dateFormat: "Y-m-d",
                altInput: true,
                altFormat: "d-m-Y",
                allowInput: true
            })

            flatpickr("#fechaHastaProduccion", {
                dateFormat: "Y-m-d",
                altInput: true,
                altFormat: "d-m-Y",
                allowInput: true
            })
        }

        bindEvents() {
            if (!this._clickHandler) {
                this._clickHandler = (e) => {

                    if (e.target.id === "btnExportarRegistrosProduccion") {
                        window.ExcelExporter.exportTable({
                            tableSelector: "#tablaRegistrosProduccion",
                            fileName: `qcc_registros_produccion_${Date.now()}.xlsx`,
                            sheetName: "Produccion",
                            title: "QCC - Registros Producción"
                        })
                    }

                    if (e.target.id === "btnActualizarRegistrosProduccion") {
                        this.filtro = this.getVal("filtroRegistrosProduccion")
                        this.cargarDatos()
                    }

                    if (e.target.id === "btnFiltrarRegistrosProduccion") {
                        this.filtro = this.getVal("filtroRegistrosProduccion")
                        this.cargarDatos()
                    }

                    if (e.target.id === "btnLimpiarRegistrosProduccion") {
                        this.filtro = ""

                        const input = document.getElementById("filtroRegistrosProduccion")
                        const desde = document.getElementById("fechaDesdeProduccion")
                        const hasta = document.getElementById("fechaHastaProduccion")

                        if (input) input.value = ""
                        if (desde?._flatpickr) desde._flatpickr.clear()
                        if (hasta?._flatpickr) hasta._flatpickr.clear()

                        this.cargarDatos()
                    }
                }

                document.addEventListener("click", this._clickHandler)
            }

            if (!this._inputHandler) {
                this._inputHandler = (e) => {
                    if (e.target.id === "filtroRegistrosProduccion") {
                        this.filtro = e.target.value || ""
                        this.renderRegistros()
                    }
                }

                document.addEventListener("input", this._inputHandler)
            }
        }

        async cargarDatos() {
            if (this.loading) return

            this.loading = true
            this.renderLoading()

            try {
                const res = await window.PhotinoBridge.send({
                    action: "registrosProduccion.obtenerResumen",
                    data: {}
                })

                if (!res || res.ok === false) {
                    throw new Error(res?.error || "Error cargando registros producción")
                }

                this.data = res.data

                this.renderKpis()
                this.renderRegistros()
            } catch (err) {
                console.error("REGISTROS PRODUCCION ERROR:", err)
                this.renderError(err.message)
            } finally {
                this.loading = false
            }
        }

        renderKpis() {
            if (!this.data) return

            this.setText("prodTotalRegistros", this.data.totalRegistros ?? 0)
            this.setText("prodRegistrosHoy", this.data.registrosHoy ?? 0)
            this.setText("prodMaquinasConRegistros", this.data.maquinasConRegistros ?? 0)
            this.setText("prodRechazos", this.data.rechazos ?? 0)
        }

        renderRegistros() {
            const tbody = document.getElementById("tbodyRegistrosProduccion")
            if (!tbody) return

            const registros = this.data?.registros || []
            const filtro = String(this.filtro || "").trim().toLowerCase()

            const filtrados = registros.filter(r => {
                if (!filtro) return true

                const texto = [
                    r.id,
                    r.fechaRegistro,
                    r.horaRegistro,
                    r.usuario,
                    r.proceso,
                    r.maquina,
                    r.formulario,
                    r.np,
                    r.producto,
                    r.turno,
                    r.estado,
                    r.observacion
                ].join(" ").toLowerCase()

                return texto.includes(filtro)
            })

            if (!filtrados.length) {
                tbody.innerHTML = `
                    <tr>
                        <td colspan="12">Sin registros de producción disponibles</td>
                    </tr>
                `
                return
            }

            tbody.innerHTML = filtrados.map(r => `
                <tr>
                    <td>${r.id}</td>
                    <td>${this.escape(r.fechaRegistro)}</td>
                    <td>${this.escape(r.horaRegistro)}</td>
                    <td>${this.escape(r.usuario)}</td>
                    <td>${this.escape(r.proceso)}</td>
                    <td>${this.escape(r.maquina)}</td>
                    <td>${this.escape(r.formulario || "-")}</td>
                    <td>${this.escape(r.np || "-")}</td>
                    <td>${this.escape(r.producto || "-")}</td>
                    <td>${this.escape(r.turno || "-")}</td>
                    <td>${this.renderEstado(r.estado)}</td>
                    <td>${this.escape(r.observacion || "-")}</td>
                </tr>
            `).join("")
        }

        renderEstado(estado) {
            const value = estado || "-"
            return `<span style="font-weight:600;">${this.escape(value)}</span>`
        }

        renderLoading() {
            const tbody = document.getElementById("tbodyRegistrosProduccion")
            if (tbody) {
                tbody.innerHTML = `
                    <tr>
                        <td colspan="12">Cargando registros de producción...</td>
                    </tr>
                `
            }
        }

        renderError(message) {
            const tbody = document.getElementById("tbodyRegistrosProduccion")
            if (tbody) {
                tbody.innerHTML = `
                    <tr>
                        <td colspan="12">Error: ${this.escape(message)}</td>
                    </tr>
                `
            }
        }

        setText(id, value) {
            const el = document.getElementById(id)
            if (el) el.textContent = value
        }

        getVal(id) {
            return document.getElementById(id)?.value || ""
        }

        escape(value) {
            return String(value ?? "")
                .replaceAll("&", "&amp;")
                .replaceAll("<", "&lt;")
                .replaceAll(">", "&gt;")
                .replaceAll('"', "&quot;")
                .replaceAll("'", "&#039;")
        }

        destroy() {
            console.log("DESTROY REGISTROS PRODUCCION")

            if (this._clickHandler) {
                document.removeEventListener("click", this._clickHandler)
            }

            if (this._inputHandler) {
                document.removeEventListener("input", this._inputHandler)
            }

            this._clickHandler = null
            this._inputHandler = null
            this.loading = false
        }
    }

    window.RegistrosProduccionController = RegistrosProduccionController
}
