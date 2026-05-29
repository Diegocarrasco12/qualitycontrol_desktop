if (!window.InicioController) {

    class InicioController {

        constructor() {
            this.chartMovimientos = null
            this.chartDistribucion = null
        }

        async init() {

            console.log("🚀 INIT INICIO QCC")

            this.bindModuleCards()

            const data = await this.cargarKPIs()

            this.renderGraficos(data)
        }

        // =========================
        // NAVEGACIÓN MÓDULOS
        // =========================

        bindModuleCards() {

            document
                .querySelectorAll(".home-module-card")
                .forEach(card => {

                    card.addEventListener("click", () => {

                        const target =
                            card.dataset.moduleTarget

                        if (!target) return

                        console.log("📦 Navegando módulo:", target)

                        if (window.app?.loadModule) {
                            window.app.loadModule(target)
                        }
                    })
                })
        }

        // =========================
        // KPIs
        // =========================

        async cargarKPIs() {

            try {

                const res =
                    await window.PhotinoBridge.send({
                        action: "inicio.getDashboard"
                    })

                console.log("📩 RESPONSE:", res)

                if (!res || res.ok === false) {

                    console.error(
                        "❌ Error backend:",
                        res?.error
                    )

                    return null
                }

                const data = res.data || {}

                console.log("🔥 DATA FINAL:", data)

                // =========================
                // KPIs SUPERIORES
                // =========================

                const totalRegistros =
                    Number(data.total?.bins || 0)

                const totalCalidad =
                    Number(data.total?.lavado || 0)

                const totalProduccion =
                    Number(data.total?.palets || 0)

                const maquinas =
                    Number(data.total?.consumo || 0)

                const usuarios =
                    Number(data.total?.altillo || 0)

                // =========================
                // ASIGNAR
                // =========================

                const elTotal =
                    document.getElementById("bins-count")

                if (elTotal)
                    elTotal.innerText = totalRegistros

                const elCalidad =
                    document.getElementById("lavado-count")

                if (elCalidad)
                    elCalidad.innerText = totalCalidad

                const elProduccion =
                    document.getElementById("palets-count")

                if (elProduccion)
                    elProduccion.innerText = totalProduccion

                const elMaquinas =
                    document.getElementById("consumo-count")

                if (elMaquinas)
                    elMaquinas.innerText = maquinas

                const elUsuarios =
                    document.getElementById("altillo-count")

                if (elUsuarios)
                    elUsuarios.innerText = usuarios

                return data

            } catch (err) {

                console.error(
                    "❌ Error cargando KPIs:",
                    err
                )

                return null
            }
        }

        // =========================
        // RENDER GENERAL
        // =========================

        renderGraficos(data) {

            if (!data) return

            this.renderGraficoMovimientos(data)

            this.renderGraficoDistribucion(data)

            this.renderActividad(
                data.actividad || []
            )

            this.renderAlertas(
                data.alertas || []
            )
        }

        // =========================
        // GRÁFICO 7 DÍAS
        // =========================

        renderGraficoMovimientos(data) {

            const ctx =
                document.getElementById("chart-movimientos")

            if (!ctx) return

            if (this.chartMovimientos) {
                this.chartMovimientos.destroy()
            }

            const calidad =
                Number(data.total?.lavado || 0)

            const produccion =
                Number(data.total?.palets || 0)

            this.chartMovimientos =
                new Chart(ctx, {

                    type: "bar",

                    data: {
                        labels: [
                            "Calidad",
                            "Producción"
                        ],

                        datasets: [{
                            label: "Registros",
                            data: [
                                calidad,
                                produccion
                            ],
                            backgroundColor: [
                                "#10b981",
                                "#f59e0b"
                            ],
                            borderRadius: 8
                        }]
                    },

                    options: {
                        responsive: true,
                        maintainAspectRatio: false,

                        plugins: {
                            legend: {
                                display: false
                            }
                        },

                        scales: {
                            y: {
                                beginAtZero: true
                            }
                        }
                    }
                })
        }

        // =========================
        // DISTRIBUCIÓN ESTADOS
        // =========================

        renderGraficoDistribucion(data) {

            const ctx =
                document.getElementById(
                    "chart-distribucion"
                )

            if (!ctx) return

            if (this.chartDistribucion) {
                this.chartDistribucion.destroy()
            }

            const estados =
                data.estadosHoy || {}

            this.chartDistribucion =
                new Chart(ctx, {

                    type: "doughnut",

                    data: {

                        labels: [
                            "Aprobados",
                            "Rechazados",
                            "Observados"
                        ],

                        datasets: [{

                            data: [

                                estados.aprobados || 0,

                                estados.rechazados || 0,

                                estados.observados || 0
                            ],

                            backgroundColor: [
                                "#10b981",
                                "#ef4444",
                                "#f59e0b"
                            ],

                            borderWidth: 0
                        }]
                    },

                    options: {

                        responsive: true,

                        maintainAspectRatio: false,

                        plugins: {

                            legend: {
                                position: "bottom"
                            }
                        }
                    }
                })
        }

        // =========================
        // ACTIVIDAD
        // =========================

        renderActividad(actividad) {

            const container =
                document.getElementById(
                    "actividad-reciente"
                )

            if (!container) return

            container.innerHTML = ""

            if (!actividad.length) {

                container.innerHTML = `
                    <div class="activity-item">
                        <span>Sin actividad reciente</span>
                        <span>-</span>
                    </div>
                `

                return
            }

            actividad.forEach(item => {

                const div =
                    document.createElement("div")

                div.className = "activity-item"

                div.innerHTML = `
                    <span>${item.descripcion || "-"}</span>
                    <span>${this.formatearTiempo(item.fecha)}</span>
                `

                container.appendChild(div)
            })
        }

        // =========================
        // ALERTAS
        // =========================

        renderAlertas(alertas) {

            const container =
                document.getElementById(
                    "alertas-list"
                )

            if (!container) return

            container.innerHTML = ""

            if (!alertas.length) {

                container.innerHTML = `
                    <div class="alert-ok">
                        ✔ Sistema sin alertas
                    </div>
                `

                return
            }

            alertas.forEach(alerta => {

                const div =
                    document.createElement("div")

                div.className = "alert-item"

                div.innerText = alerta

                container.appendChild(div)
            })
        }

        // =========================
        // TIEMPO RELATIVO
        // =========================

        formatearTiempo(fecha) {

            if (!fecha) return "-"

            const f =
                new Date(fecha.replace(" ", "T"))

            if (isNaN(f)) return "-"

            const now = new Date()

            const diff =
                Math.floor((now - f) / 60000)

            if (diff < 1)
                return "Ahora"

            if (diff < 60)
                return `Hace ${diff} min`

            const horas =
                Math.floor(diff / 60)

            return `Hace ${horas} h`
        }

        // =========================
        // DESTROY
        // =========================

        destroy() {

            console.log(
                "🧹 Destroy InicioController"
            )

            if (this.chartMovimientos) {
                this.chartMovimientos.destroy()
            }

            if (this.chartDistribucion) {
                this.chartDistribucion.destroy()
            }
        }
    }

    window.InicioController =
        InicioController
}
