// ==============================
// ALTILLO CONTROLLER (FINAL ESTABLE)
// ==============================

if (!window.AltilloController) {

    class AltilloController {

        constructor() {
            this.data = []
            this.paginaActual = 1
            this.registrosPorPagina = 20
            this.totalPaginas = 1

            this.filtros = {
                fechaDesde: "",
                fechaHasta: "",
                codigo: "",
                lote: ""
            }

            this.cambiosPendientes = {}
            this._eventsBound = false
            this.exportando = false
        }

        async init() {
            console.log("🚀 INIT ALTILLO")

            this.initDatePickers()
            this.bindEvents()
            await this.cargarDatos()
            await new Promise(r => requestAnimationFrame(r))
        }

        initDatePickers() {
            if (typeof flatpickr === "undefined") return
        
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

        bindEvents() {

            if (this._eventsBound) return
            this._eventsBound = true

            // =========================
            // BOTONES
            // =========================
            if (!this._clickHandler) {

                this._clickHandler = (e) => {
                    if (e.target.id === "btnFiltrar") this.aplicarFiltros()
                    if (e.target.id === "btnLimpiar") this.limpiarFiltros()
                    if (e.target.id === "btnActualizar") this.cargarDatos()
                    if (e.target.id === "btnExportar") this.exportarExcel()
                    if (e.target.id === "btnGuardar") this.guardarCambios()
                }

                document.addEventListener("click", this._clickHandler)
            }

            // =========================
            // INPUT (GUARDADO REAL)
            // =========================
            const tabla = document.getElementById("tablaAltillo")

            tabla?.addEventListener("input", (e) => {

                if (!e.target.classList.contains("input-table")) return

                const id = e.target.dataset.id
                const field = e.target.dataset.field

                if (!id || !field) return

                const filaOriginal = this.data.find(r => String(r.id) === String(id)) || {}

                if (!this.cambiosPendientes[id]) {
                    this.cambiosPendientes[id] = {
                        comentario: filaOriginal.comentario ?? "",
                        estado: filaOriginal.estado ?? "",
                        extraPostEstado: filaOriginal.extraPostEstado ?? ""
                    }
                }

                // 🔥 CLAVE: MERGE (NO PISAR)
                this.cambiosPendientes[id] = {
                    ...this.cambiosPendientes[id],
                    [field]: e.target.value
                }

                e.target.closest("tr")?.classList.add("row-editada")
            })

            // =========================
            // PEGADO EXCEL (VERTICAL)
            // =========================
            tabla?.addEventListener("paste", async (e) => {

                const input = e.target

                if (!input.classList.contains("input-table")) return

                const text = e.clipboardData.getData("text")

                if (!text.includes("\n")) return

                e.preventDefault()

                const values = text
                    .replace(/\r/g, "")
                    .split("\n")
                    .filter(v => v.trim() !== "")

                let row = input.closest("tr")
                let colIndex = input.closest("td").cellIndex

                for (const value of values) {

                    if (!row) break

                    const cell = row.cells[colIndex]
                    const inp = cell?.querySelector("input")

                    if (inp) {

                        inp.value = value

                        // 🔥 IMPORTANTE
                        inp.dispatchEvent(new Event("input", { bubbles: true }))

                        // 🔥 CLAVE: esperar a que el sistema registre el cambio
                        await new Promise(resolve => setTimeout(resolve, 0))
                    }

                    row = row.nextElementSibling
                }

                console.log("✅ Paste OK")
            })
        }

        aplicarFiltros() {
            this.filtros.fechaDesde = document.getElementById("filtroFechaDesde")?.value || ""
            this.filtros.fechaHasta = document.getElementById("filtroFechaHasta")?.value || ""
            this.filtros.codigo = document.getElementById("filtroCodigo")?.value || ""
            this.filtros.lote = document.getElementById("filtroLote")?.value || ""

            this.paginaActual = 1
            this.cargarDatos()
        }

        limpiarFiltros() {

            document.getElementById("filtroFechaDesde").value = ""
            document.getElementById("filtroFechaHasta").value = ""
            document.getElementById("filtroCodigo").value = ""
            document.getElementById("filtroLote").value = ""

            this.filtros = {
                fechaDesde: "",
                fechaHasta: "",
                codigo: "",
                lote: ""
            }

            this.paginaActual = 1
            this.cargarDatos()
        }

        // =========================
        // CARGAR DATOS
        // =========================
        async cargarDatos() {

            try {

                const res = await window.PhotinoBridge.send({
                    action: "altillo.obtenerRegistros",
                    fechaDesde: this.filtros.fechaDesde,
                    fechaHasta: this.filtros.fechaHasta,
                    codigo: this.filtros.codigo,
                    lote: this.filtros.lote,
                    page: this.paginaActual,
                    limit: this.registrosPorPagina
                })

                if (!res || res.ok === false) {
                    throw new Error(res?.error || "Error backend")
                }

                this.data = (res.data?.data || []).map(r => ({
                    ...r,
                    extraPostEstado: r.extraPostEstado ?? r.extra_post_estado ?? ""
                }))
                this.totalPaginas = res.data?.pages || 1
                this.cambiosPendientes = {}

                this.renderTabla()
                this.renderPaginacion()
                await this.cargarKpis()

            } catch (err) {
                console.error("❌ Error altillo:", err)
                this.renderError(err.message)
            }
        }
        async cargarKpis() {

            try {

                const res = await window.PhotinoBridge.send({
                    action: "altillo.obtenerKpis"
                })

                console.log("📊 KPIs:", res)

                if (!res || res.ok === false) return

                const kpis = res.data

                const elTotal = document.getElementById("kpiTotalRegistros")
                const elSaldo = document.getElementById("kpiSaldoTotal")
                const elUltimo = document.getElementById("kpiUltimoRegistro")

                if (!elTotal || !elSaldo || !elUltimo) {
                    console.warn("⚠️ KPI DOM no listo")
                    return
                }

                elTotal.innerText = kpis.totalRegistros ?? "--"
                elSaldo.innerText = this.formatNumero(kpis.saldoTotal)
                elUltimo.innerText = kpis.ultimoRegistro
                    ? this.formatFecha(kpis.ultimoRegistro)
                    : "--"

            } catch (err) {
                console.error("❌ Error KPIs:", err)
            }
        }

        // =========================
        // GUARDAR (FIX REAL)
        // =========================
        async guardarCambios() {

            // 🔥 AGREGA ESTA LÍNEA
            await new Promise(resolve => setTimeout(resolve, 50))

            const cambios = Object.entries(this.cambiosPendientes)

                .map(([id, values]) => ({
                    id: Number(id),
                    comentario: values.comentario ?? "",
                    estado: values.estado ?? "",
                    extraPostEstado: values.extraPostEstado ?? ""
                }))
                .filter(c => c.id > 0)

            if (!cambios.length) return

            console.log("📤 cambios:", cambios)

            try {

                const res = await window.PhotinoBridge.send({
                    action: "altillo.guardarCambios",
                    data: cambios
                })

                if (!res || res.ok === false) {
                    throw new Error(res?.error || "Error backend")
                }

                alert("✅ Cambios guardados correctamente")

                this.cambiosPendientes = {}

                await this.cargarDatos()

            } catch (err) {
                console.error("❌ Error guardar:", err)
                alert("Error al guardar")
            }
        }

        // =========================
        // EXPORTAR
        // =========================
        async exportarExcel() {

            // 🔴 BLOQUEO DOBLE CLICK
            if (this.exportando) return
        
            this.exportando = true
        
            try {
        
                const res = await window.PhotinoBridge.send({
                    action: "altillo.exportarExcel",
                    fechaDesde: this.filtros.fechaDesde,
                    fechaHasta: this.filtros.fechaHasta,
                    codigo: this.filtros.codigo,
                    lote: this.filtros.lote
                })
        
                if (!res || res.ok === false) {
                    throw new Error(res?.error || "Error exportando")
                }
        
                // 🔥 FIX RESPUESTA CORRECTA
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
        // TABLA
        // =========================
        renderTabla() {

            const tbody = document.getElementById("tablaAltillo")
            if (!tbody) return

            if (!this.data.length) {
                tbody.innerHTML = `<tr><td colspan="13">Sin datos</td></tr>`
                return
            }

            tbody.innerHTML = this.data.map(r => `
                <tr>
                    <td>${r.id}</td>
                    <td>${this.formatFecha(r.fecha)}</td>
                    <td>${r.nombre ?? ""}</td>
                    <td>${r.descripcion ?? ""}</td>
                    <td>${r.codigo ?? ""}</td>
                    <td>${this.formatNumero(r.consumo)}</td>
                    <td>${r.np ?? ""}</td>
                    <td>${this.formatNumero(r.unidadesTarja)}</td>
                    <td>${this.formatNumero(r.saldo)}</td>
                    <td>${r.lote ?? ""}</td>

                    <td><input class="input-table" data-id="${r.id}" data-field="comentario" value="${r.comentario ?? ""}"></td>
                    <td><input class="input-table" data-id="${r.id}" data-field="estado" value="${r.estado ?? ""}"></td>
                    <td><input class="input-table" data-id="${r.id}" data-field="extraPostEstado" value="${r.extraPostEstado ?? r.extra_post_estado ?? ""}"></td>
                </tr>
            `).join("")
        }

        renderPaginacion() {

            const container = document.getElementById("pagination")
            if (!container) return

            let html = ""

            const rango = 2 // 👈 cantidad de páginas a cada lado

            let inicio = Math.max(1, this.paginaActual - rango)
            let fin = Math.min(this.totalPaginas, this.paginaActual + rango)

            // 👈 botón primera
            if (inicio > 1) {
                html += `<button data-page="1">1</button>`
                if (inicio > 2) html += `<span>...</span>`
            }

            for (let i = inicio; i <= fin; i++) {
                html += `
        <button data-page="${i}" 
            class="${i === this.paginaActual ? "active" : ""}">
            ${i}
        </button>
    `
            }

            // 👈 botón última
            if (fin < this.totalPaginas) {
                if (fin < this.totalPaginas - 1) html += `<span>...</span>`
                html += `<button data-page="${this.totalPaginas}">${this.totalPaginas}</button>`
            }

            container.innerHTML = html

            container.querySelectorAll("button").forEach(btn => {
                btn.addEventListener("click", () => {
                    this.paginaActual = parseInt(btn.dataset.page)
                    this.cargarDatos()
                })
            })
        }

        formatFecha(f) {
            if (!f) return ""

            const datePart = f.split("T")[0]
            const [year, month, day] = datePart.split("-")

            return `${day}-${month}-${year}`
        }

        formatNumero(n) {
            if (n == null) return ""
            return Number(n).toLocaleString("es-CL")
        }

        renderError(msg) {
            const tbody = document.getElementById("tablaAltillo")
            if (!tbody) return
            tbody.innerHTML = `<tr><td colspan="13">${msg}</td></tr>`
        }
        
        destroy() {
            console.log("🧹 Destroy AltilloController")
        
            if (this._clickHandler) {
                document.removeEventListener("click", this._clickHandler)
            }
        
            this._eventsBound = false
            this._clickHandler = null
            this.cambiosPendientes = {}
        }
        
        }
        
        window.AltilloController = AltilloController
        }