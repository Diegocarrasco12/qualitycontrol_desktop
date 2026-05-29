if (!window.DashboardController) {
  class DashboardController {
    constructor() {
      this.data = null
      this.loading = false
      this._clickHandler = null
    }

    async init() {
      console.log("INIT DASHBOARD")
      this.bindEvents()
      this.initDatePickers()
      await this.cargarDatos()
    }
    initDatePickers() {
      if (!window.flatpickr) return

      flatpickr("#dashboardFechaDesde", {
        dateFormat: "Y-m-d",
        altInput: true,
        altFormat: "d-m-Y",
        allowInput: true
      })

      flatpickr("#dashboardFechaHasta", {
        dateFormat: "Y-m-d",
        altInput: true,
        altFormat: "d-m-Y",
        allowInput: true
      })
    }

    bindEvents() {
      if (this._clickHandler) return

      this._clickHandler = (e) => {
        if (e.target.id === "btnActualizarDashboard") {
          this.cargarDatos()
        }
        if (e.target.id === "btnFiltrarDashboard") {
          this.cargarDatos()
        }

        if (e.target.id === "btnLimpiarDashboard") {

          const desde = document.getElementById("dashboardFechaDesde")
          const hasta = document.getElementById("dashboardFechaHasta")

          if (desde?._flatpickr) desde._flatpickr.clear()
          if (hasta?._flatpickr) hasta._flatpickr.clear()

          this.cargarDatos()
        }
        if (e.target.id === "btnExportarDashboard") {
          window.ExcelExporter.exportTable({
            tableSelector: "#tablaDashboardUltimos",
            fileName: "qcc_dashboard_calidad.xlsx",
            sheetName: "Dashboard",
            title: "QCC - Dashboard Calidad"
          })
        }
      }

      document.addEventListener("click", this._clickHandler)
    }

    async cargarDatos() {
      if (this.loading) return

      this.loading = true
      this.renderLoading()

      try {
        const res = await window.PhotinoBridge.send({
          action: "dashboard.obtenerResumen",
          data: {
            fechaDesde: this.getVal("dashboardFechaDesde"),
            fechaHasta: this.getVal("dashboardFechaHasta")
          }
        })

        if (!res || res.ok === false) {
          throw new Error(res?.error || "Error cargando dashboard")
        }

        this.data = res.data
        this.render()
      } catch (err) {
        console.error("DASHBOARD ERROR:", err)
        this.renderError(err.message)
      } finally {
        this.loading = false
      }
    }

    render() {
      if (!this.data) return

      this.setText("dashControlesHoy", this.data.controlesHoy ?? 0)
      this.setText("dashMermaInsumos", this.formatNumber(this.data.mermaInsumosHoy ?? 0))
      this.setText("dashMermaProceso", this.formatNumber(this.data.mermaProcesoHoy ?? 0))
      this.setText("dashObservaciones", this.data.registrosConObservacionHoy ?? 0)

      this.renderUltimosRegistros()
    }

    renderUltimosRegistros() {
      const tbody = document.getElementById("tbodyDashboardUltimos")
      if (!tbody) return

      const registros = this.data?.ultimosRegistros || []

      if (!registros.length) {
        tbody.innerHTML = `
            <tr>
              <td colspan="12">Sin registros disponibles</td>
            </tr>
          `
        return
      }

      tbody.innerHTML = registros.map(r => `
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
      const tbody = document.getElementById("tbodyDashboardUltimos")
      if (tbody) {
        tbody.innerHTML = `
            <tr>
              <td colspan="12">Cargando dashboard...</td>
            </tr>
          `
      }
    }

    renderError(message) {
      const tbody = document.getElementById("tbodyDashboardUltimos")
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

    formatNumber(value) {
      const n = Number(value || 0)

      return n.toLocaleString("es-CL", {
        minimumFractionDigits: 0,
        maximumFractionDigits: 2
      })
    }

    escape(value) {
      return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;")
    }

    getVal(id) {
      return document.getElementById(id)?.value || ""
    }

    destroy() {
      console.log("DESTROY DASHBOARD")

      if (this._clickHandler) {
        document.removeEventListener("click", this._clickHandler)
      }

      this._clickHandler = null
      this.loading = false
    }
  }

  window.DashboardController = DashboardController
}
