if (!window.RegistrosControlController) {
  class RegistrosControlController {
    constructor() {
      this.data = []
      this.page = 1
      this.limit = 20
      this.pages = 1
      this.total = 0
      this.loading = false
      this._clickHandler = null
    }

    async init() {
      console.log("INIT REGISTROS CONTROL")
      this.bindEvents()
      this.initDatePickers()
      await this.cargarDatos()
    }

    initDatePickers() {
      if (!window.flatpickr) return

      flatpickr("#fechaDesdeRegistros", {
        dateFormat: "Y-m-d",
        altInput: true,
        altFormat: "d-m-Y",
        allowInput: true
      })

      flatpickr("#fechaHastaRegistros", {
        dateFormat: "Y-m-d",
        altInput: true,
        altFormat: "d-m-Y",
        allowInput: true
      })
    }

    bindEvents() {
      if (this._clickHandler) return

      this._clickHandler = (e) => {
        if (e.target.id === "btnBuscarRegistros") {
          this.page = 1
          this.cargarDatos()
        }
        if (e.target.id === "btnExportarRegistrosControl") {
          window.ExcelExporter.exportTable({
            tableSelector: "#tablaRegistrosControl",
            fileName: `qcc_registros_calidad_${Date.now()}.xlsx`,
            sheetName: "Registros Calidad",
            title: "QCC - Registros de Calidad"
          })
        }

        if (e.target.id === "btnLimpiarRegistros") {
          this.limpiarFiltros()
          this.page = 1
          this.cargarDatos()
        }

        if (e.target.dataset.registrosPage) {
          this.page = Number(e.target.dataset.registrosPage)
          this.cargarDatos()
        }
      }

      document.addEventListener("click", this._clickHandler)
    }

    async cargarDatos() {
      if (this.loading) return

      this.loading = true
      this.renderLoading()

      try {
        const payload = {
          page: this.page,
          limit: this.limit,
          fechaDesde: this.getVal("fechaDesdeRegistros"),
          fechaHasta: this.getVal("fechaHastaRegistros"),
          np: this.getVal("filtroNpRegistros"),
          turno: this.getVal("filtroTurnoRegistros"),
          estado: this.getVal("filtroEstadoRegistros")
        }

        const res = await window.PhotinoBridge.send({
          action: "registrosControl.obtenerRegistros",
          data: payload
        })

        if (!res || res.ok === false) {
          throw new Error(res?.error || "Error cargando registros de control")
        }

        this.data = res.data.items || []
        this.total = res.data.total || 0
        this.pages = res.data.pages || 1
        this.page = res.data.page || 1

        this.render()
        this.renderPaginacion()
        this.renderKpis()
      } catch (err) {
        console.error("REGISTROS CONTROL ERROR:", err)
        this.renderError(err.message)
      } finally {
        this.loading = false
      }
    }

    render() {
      const tbody = document.getElementById("tbodyRegistrosControl")
      if (!tbody) return

      if (!this.data.length) {
        tbody.innerHTML = `
            <tr>
              <td colspan="11">Sin registros para los filtros seleccionados</td>
            </tr>
          `
        return
      }

      tbody.innerHTML = this.data.map(r => `
          <tr>
            <td>${r.id}</td>
            <td>${this.escape(r.fechaRegistro)}</td>
            <td>${this.escape(r.horaRegistro)}</td>
            <td>${this.escape(r.usuario)}</td>
            <td>${this.escape(r.proceso)}</td>
            <td>${this.escape(r.maquina)}</td>
            <td>${this.escape(r.formulario || "-")}</td>
            <td>${this.escape(r.np || "-")}</td>
            <td>${this.escape(r.turno)}</td>
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
      const tbody = document.getElementById("tbodyRegistrosControl")
      if (!tbody) return

      tbody.innerHTML = `
          <tr>
            <td colspan="11">Cargando registros...</td>
          </tr>
        `
    }

    renderError(message) {
      const tbody = document.getElementById("tbodyRegistrosControl")
      if (!tbody) return

      tbody.innerHTML = `
          <tr>
            <td colspan="11">Error: ${this.escape(message)}</td>
          </tr>
        `
    }

    renderKpis() {
      const total = document.getElementById("kpiTotalRegistros")
      const pagina = document.getElementById("kpiPaginaActual")
      const visibles = document.getElementById("kpiVisibles")

      if (total) total.textContent = this.total
      if (pagina) pagina.textContent = this.page
      if (visibles) visibles.textContent = this.data.length
    }

    renderPaginacion() {
      const container = document.getElementById("paginacionRegistrosControl")
      if (!container) return

      let html = ""
      const rango = 2
      const inicio = Math.max(1, this.page - rango)
      const fin = Math.min(this.pages, this.page + rango)

      if (this.page > 1) {
        html += `<button data-registros-page="${this.page - 1}">←</button>`
      }

      if (inicio > 1) {
        html += `<button data-registros-page="1">1</button>`
        if (inicio > 2) html += `<button disabled>...</button>`
      }

      for (let i = inicio; i <= fin; i++) {
        html += `
            <button
              data-registros-page="${i}"
              class="${i === this.page ? "active" : ""}">
              ${i}
            </button>
          `
      }

      if (fin < this.pages) {
        if (fin < this.pages - 1) html += `<button disabled>...</button>`
        html += `<button data-registros-page="${this.pages}">${this.pages}</button>`
      }

      if (this.page < this.pages) {
        html += `<button data-registros-page="${this.page + 1}">→</button>`
      }

      container.innerHTML = html
    }

    limpiarFiltros() {
      [
        "fechaDesdeRegistros",
        "fechaHastaRegistros",
        "filtroNpRegistros",
        "filtroTurnoRegistros",
        "filtroEstadoRegistros"
      ].forEach(id => {
        const el = document.getElementById(id)
        if (el) el.value = ""
        if (el && el._flatpickr) el._flatpickr.clear()
      })
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
      console.log("DESTROY REGISTROS CONTROL")

      if (this._clickHandler) {
        document.removeEventListener("click", this._clickHandler)
      }

      this._clickHandler = null
      this.loading = false
    }
  }

  window.RegistrosControlController = RegistrosControlController
}
