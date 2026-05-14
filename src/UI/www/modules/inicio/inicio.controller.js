if (!window.InicioController) {

    class InicioController {
        constructor() {
            this.chartMovimientos = null
            this.chartDistribucion = null
        }

        async init() {
            console.log("🚀 INIT INICIO")

            const data = await this.cargarKPIs()
            this.renderGraficos(data)
        }

        async cargarKPIs() {
            try {
                const res = await window.PhotinoBridge.send({
                    action: "inicio.getDashboard"
                })

                console.log("📩 RESPONSE:", res)

                if (!res || res.ok === false) {
                    console.error("❌ Error backend:", res?.error)
                    return null
                }

                const data = res.data || {}

                console.log("🔥 DATA FINAL:", data)

                document.getElementById("bins-count").innerText = data.total?.bins || 0
                document.getElementById("lavado-count").innerText = data.total?.lavado || 0
                document.getElementById("palets-count").innerText = data.total?.palets || 0

                const paletizadoRes = await window.PhotinoBridge.send({
                    action: "paletizado.obtenerKPI"
                })

                if (paletizadoRes && paletizadoRes.ok !== false) {
                    const el = document.getElementById("paletizado-count")
                    if (el) el.innerText = paletizadoRes.data?.total || 0
                }

                document.getElementById("consumo-count").innerText = data.total?.consumo || 0
                document.getElementById("altillo-count").innerText = data.total?.altillo || 0
                return data

            } catch (err) {
                console.error("❌ Error cargando KPIs:", err)
                return null
            }
        }

        // =========================
        // GRÁFICOS
        // =========================
        renderGraficos(data) {
            if (!data) return

            this.renderGraficoMovimientos(data)
            this.renderGraficoDistribucion(data)


            this.renderActividad(data.actividad || [])
            this.renderAlertas(data.alertas || [])
        }

        renderGraficoMovimientos(data) {
            const ctx = document.getElementById("chart-movimientos")

            if (!ctx) {
                console.warn("⚠️ No existe canvas chart-movimientos")
                return
            }

            // 🔥 EVITAR DUPLICADOS
            if (this.chartMovimientos) {
                this.chartMovimientos.destroy()
            }

            this.chartMovimientos = new Chart(ctx, {
                type: 'bar',
                data: {
                    labels: ["Bins", "Lavado", "Palets", "Consumo", "Altillo"],
                    datasets: [{
                        label: "Movimientos",
                        data: [
                            data.semana?.bins || 0,
                            data.semana?.lavado || 0,
                            data.semana?.palets || 0,
                            data.semana?.consumo || 0,
                            data.semana?.altillo || 0
                        ],
                        backgroundColor: [
                            "#3b82f6", // azul
                            "#10b981", // verde
                            "#f59e0b", // amarillo
                            "#ef4444", // rojo
                            "#8b5cf6"  // morado
                        ],
                        borderRadius: 6
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    animation: {
                        duration: 800
                    },
                    plugins: {
                        legend: {
                            display: false
                        }
                    }
                }
            })
        }

        renderGraficoDistribucion(data) {
            const ctx = document.getElementById("chart-distribucion")

            if (!ctx) {
                console.warn("⚠️ No existe canvas chart-distribucion")
                return
            }

            // 🔥 EVITAR DUPLICADOS
            if (this.chartDistribucion) {
                this.chartDistribucion.destroy()
            }

            this.chartDistribucion = new Chart(ctx, {
                type: 'doughnut',
                data: {
                    labels: ["Bins", "Lavado", "Palets", "Consumo", "Altillo"],
                    datasets: [{
                        data: [
                            data.hoy?.bins || 0,
                            data.hoy?.lavado || 0,
                            data.hoy?.palets || 0,
                            data.hoy?.consumo || 0,
                            data.hoy?.altillo || 0
                        ],
                        backgroundColor: [
                            "#3b82f6",
                            "#10b981",
                            "#f59e0b",
                            "#ef4444",
                            "#8b5cf6"
                        ],
                        borderWidth: 0
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    animation: {
                        duration: 800
                    },
                    plugins: {
                        legend: {
                            position: 'bottom'
                        }
                    }
                }
            })
        }
        renderActividad(actividad) {
            const container = document.getElementById("actividad-reciente")

            if (!container) {
                console.warn("⚠️ No existe actividad-reciente")
                return
            }

            container.innerHTML = ""

            actividad.forEach(item => {

                const div = document.createElement("div")
                div.className = "activity-item"

                div.innerHTML = `
                    <span>${item.descripcion}</span>
                    <span>${this.formatearTiempo(item.fecha)}</span>
                `

                container.appendChild(div)
            })
        }
        renderAlertas(alertas) {
            const container = document.getElementById("alertas-list")

            if (!container) {
                console.warn("⚠️ No existe alertas-list")
                return
            }

            container.innerHTML = ""

            if (!alertas.length) {
                container.innerHTML = `<div class="alert-ok">✔ Sistema sin problemas</div>`
                return
            }

            alertas.forEach(a => {
                const div = document.createElement("div")
                div.className = "alert-item"
                div.innerText = a
                container.appendChild(div)
            })
        }
        formatearTiempo(fecha) {
            if (!fecha) return "-"

            // 🔥 FIX formato MySQL (yyyy-MM-dd HH:mm:ss)
            const f = new Date(fecha.replace(" ", "T"))

            if (isNaN(f)) return "-"

            const now = new Date()
            const diff = Math.floor((now - f) / 60000)

            if (diff < 1) return "Ahora"
            if (diff < 60) return `Hace ${diff} min`

            const horas = Math.floor(diff / 60)
            return `Hace ${horas} h`
        }

        destroy() {
            console.log("🧹 Destroy InicioController")
        }
    }

    window.InicioController = InicioController
}