if (!window.DashboardController) {
  class DashboardController {
    constructor() {
      this.data = null
      this.loading = false
      this._clickHandler = null
      this.charts = []
    }

    async init() {
      console.log("INIT DASHBOARD CALIDAD")

      this.bindEvents()
      this.initDatePickers()

      await this.cargarFiltros()
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
          document.getElementById("dashboardInspector").value = ""
          document.getElementById("dashboardTurno").value = ""
          document.getElementById("dashboardProceso").value = ""

          this.cargarDatos()
        }

        if (e.target.id === "btnExportarDashboard") {
          window.ExcelExporter.exportTable({
            tableSelector: "#tablaDashboardDesempeno",
            fileName: "qcc_dashboard_calidad.xlsx",
            sheetName: "Dashboard Calidad",
            title: "QCC - Dashboard Calidad"
          })
        }
      }

      document.addEventListener("click", this._clickHandler)
    }

    async cargarFiltros() {
      try {
        const res = await window.PhotinoBridge.send({
          action: "dashboard.obtenerFiltros"
        })

        if (!res || res.ok === false) {
          throw new Error(res?.error || "Error cargando filtros")
        }

        const usuarios = res.data?.usuarios || []
        const procesos = res.data?.procesos || []

        const cboInspector = document.getElementById("dashboardInspector")
        const cboProceso = document.getElementById("dashboardProceso")

        if (cboInspector) {
          cboInspector.innerHTML =
            `<option value="">Todas</option>` +
            usuarios.map(x =>
              `<option value="${x.id}">${x.nombre}</option>`
            ).join("")
        }

        if (cboProceso) {
          cboProceso.innerHTML =
            `<option value="">Todos</option>` +
            procesos.map(x =>
              `<option value="${x.id}">${x.nombre}</option>`
            ).join("")
        }

      } catch (err) {
        console.error("ERROR FILTROS:", err)
      }
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
            fechaHasta: this.getVal("dashboardFechaHasta"),
            inspector: this.getVal("dashboardInspector"),
            turno: this.getVal("dashboardTurno"),
            proceso: this.getVal("dashboardProceso")
          }
        })

        console.log("📊 DASHBOARD CALIDAD:", res)

        if (!res || res.ok === false) {
          throw new Error(res?.error || "Error cargando dashboard")
        }

        this.data = res.data || {}
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

      this.renderKpis()
      this.renderCharts()
      this.renderDesempeno()
      this.renderUltimosRegistros()
    }

    renderKpis() {
      this.setText("dashControlesHoy", this.numero(this.data.controlesHoy))
      this.setText("dashControlesPeriodo", this.numero(this.data.controlesPeriodo))
      this.setText("dashCumplimientoGeneral", `${this.numero(this.data.cumplimientoGeneral)}%`)
      this.setText("dashNoConformidades", this.numero(this.data.noConformidadesDetectadas))
    }

    renderCharts() {
      this.destroyCharts()

      this.chartBarHorizontal(
        "chartDashboardCumplimientoInspector",
        this.data.cumplimientoPorInspector || [],
        "inspector",
        "porcentaje",
        "Cumplimiento %"
      )

      this.chartBarHorizontal(
        "chartDashboardNoConformidadesInspector",
        this.data.noConformidadesPorInspector || [],
        "inspector",
        "total",
        "No conformidades"
      )

      this.chartControlesPorProceso(
        "chartDashboardControlesProceso",
        this.data.controlesPorProceso || []
      )

      this.chartLine(
        "chartDashboardTendenciaCumplimiento",
        this.data.tendenciaCumplimiento || [],
        "fecha",
        "cumplimiento",
        "Cumplimiento %"
      )
    }

    renderDesempeno() {
      const tbody = document.getElementById("tbodyDashboardDesempeno")
      if (!tbody) return

      const rows = this.data.desempenoIndividual || []

      if (!rows.length) {
        tbody.innerHTML = `
                  <tr>
                      <td colspan="6">Sin datos de desempeño</td>
                  </tr>
              `
        return
      }

      tbody.innerHTML = rows.map(r => `
              <tr>
                  <td>${this.escape(r.inspector)}</td>
                  <td>${this.numero(r.cumplimiento)}%</td>
                  <td>${this.numero(r.controlesProgramados)}</td>
                  <td>${this.numero(r.controlesRealizados)}</td>
                  <td>${this.numero(r.noConformidades)}</td>
                  <td>${this.renderEstadoDesempeno(r.estado)}</td>
              </tr>
          `).join("")
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
                  <td>${this.numero(r.id)}</td>
                  <td>${this.escape(r.fechaRegistro)}</td>
                  <td>${this.escape(r.horaRegistro)}</td>
                  <td>${this.escape(r.usuario)}</td>
                  <td>${this.escape(r.proceso)}</td>
                  <td>${this.escape(r.maquina)}</td>
                  <td>${this.escape(r.formulario || "-")}</td>
                  <td>${this.escape(r.np || "-")}</td>
                  <td>${this.escape(r.producto || "-")}</td>
                  <td>${this.escape(r.turno || "-")}</td>
                  <td>${this.escape(r.estado || "-")}</td>
                  <td>${this.escape(r.observacion || "-")}</td>
              </tr>
          `).join("")
    }

    chartBarHorizontal(canvasId, rows, labelKey, valueKey, label) {
      const ctx = document.getElementById(canvasId)
      if (!ctx) return

      const labels = rows.map(x => x[labelKey] || "-")
      const values = rows.map(x => Number(x[valueKey] || 0))

      const chart = new Chart(ctx, {
        type: "bar",
        data: {
          labels,
          datasets: [{
            label,
            data: values,
            backgroundColor: [
              "#2563eb",
              "#16a34a",
              "#f97316",
              "#dc2626",
              "#7c3aed",
              "#0891b2"
            ],
            borderRadius: 8
          }]
        },
        options: {
          indexAxis: "y",
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: { display: false }
          },
          scales: {
            x: { beginAtZero: true },
            y: { ticks: { font: { size: 11 } } }
          }
        }
      })

      this.charts.push(chart)
    }

    chartControlesPorProceso(canvasId, rows) {
      const ctx = document.getElementById(canvasId)
      if (!ctx) return

      const procesos = [...new Set(rows.map(x => x.proceso || "-"))]
      const inspectores = [...new Set(rows.map(x => x.inspector || "-"))]

      const datasets = inspectores.map(inspector => {
        return {
          label: inspector,
          data: procesos.map(proceso => {
            const row = rows.find(x =>
              (x.proceso || "-") === proceso &&
              (x.inspector || "-") === inspector
            )

            return Number(row?.total || 0)
          }),
          borderRadius: 8
        }
      })

      const chart = new Chart(ctx, {
        type: "bar",
        data: {
          labels: procesos,
          datasets
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: {
              position: "bottom",
              labels: { font: { size: 10 } }
            }
          },
          scales: {
            x: { stacked: true },
            y: {
              stacked: true,
              beginAtZero: true
            }
          }
        }
      })

      this.charts.push(chart)
    }

    chartLine(canvasId, rows, labelKey, valueKey, label) {
      const ctx = document.getElementById(canvasId)
      if (!ctx) return

      const labels = rows.map(x => x[labelKey] || "-")
      const values = rows.map(x => Number(x[valueKey] || 0))

      const chart = new Chart(ctx, {
        type: "line",
        data: {
          labels,
          datasets: [{
            label,
            data: values,
            borderColor: "#2563eb",
            backgroundColor: "rgba(37, 99, 235, 0.12)",
            pointBackgroundColor: "#2563eb",
            pointRadius: 4,
            tension: 0.35,
            fill: true
          }]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: { display: false }
          },
          scales: {
            y: {
              beginAtZero: true,
              suggestedMax: 100
            }
          }
        }
      })

      this.charts.push(chart)
    }

    renderEstadoDesempeno(estado) {
      const value = estado || "-"

      let color = "#64748b"

      if (value === "Excelente") color = "#16a34a"
      if (value === "A mejorar") color = "#f97316"
      if (value === "Crítico") color = "#dc2626"

      return `
              <span style="
                  font-weight:700;
                  color:${color};
              ">
                  ${this.escape(value)}
              </span>
          `
    }

    renderLoading() {
      const tbody1 = document.getElementById("tbodyDashboardDesempeno")
      const tbody2 = document.getElementById("tbodyDashboardUltimos")

      if (tbody1) {
        tbody1.innerHTML = `
                  <tr>
                      <td colspan="6">Cargando dashboard...</td>
                  </tr>
              `
      }

      if (tbody2) {
        tbody2.innerHTML = `
                  <tr>
                      <td colspan="12">Cargando registros...</td>
                  </tr>
              `
      }
    }

    renderError(message) {
      const tbody1 = document.getElementById("tbodyDashboardDesempeno")
      const tbody2 = document.getElementById("tbodyDashboardUltimos")

      if (tbody1) {
        tbody1.innerHTML = `
                  <tr>
                      <td colspan="6">Error: ${this.escape(message)}</td>
                  </tr>
              `
      }

      if (tbody2) {
        tbody2.innerHTML = `
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

    numero(value) {
      return Number(value || 0).toLocaleString("es-CL", {
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

    destroyCharts() {
      this.charts.forEach(chart => {
        try {
          chart.destroy()
        } catch (_) { }
      })

      this.charts = []
    }

    destroy() {
      console.log("DESTROY DASHBOARD")

      if (this._clickHandler) {
        document.removeEventListener("click", this._clickHandler)
      }

      this._clickHandler = null
      this.loading = false
      this.destroyCharts()
    }
  }

  window.DashboardController = DashboardController
}
