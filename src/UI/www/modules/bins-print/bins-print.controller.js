if (!window.BinsPrintController) {

    class BinsPrintController {

        constructor() {
            this._eventsBound = false
        }

        init() {
            console.log("🚀 INIT BINS PRINT")

            this.btn = document.getElementById("binsPrintBtn")
            this.input = document.getElementById("binsPrintInput")
            this.msg = document.getElementById("binsPrintMsg")

            if (!this.btn || !this.input || !this.msg) {
                console.error("❌ Elementos bins-print no encontrados")
                return
            }
            if (this._eventsBound) return
            this._eventsBound = true
            
            this._clickHandler = () => this.imprimir()
            
            this.btn.addEventListener("click", this._clickHandler)

            this.input.addEventListener("keydown", (e) => {
                if (e.key === "Enter") {
                    e.preventDefault()
                    this.imprimir()
                }
            })
        }

        async imprimir() {
            try {
                const bin = this.input.value.trim()

                if (!bin) {
                    this.showError("Ingresa un BIN")
                    return
                }

                console.log("📤 Enviando BIN:", bin)

                const res = await window.PhotinoBridge.send({
                    action: "binsPrint.imprimir", // 🔥 CAMBIO CLAVE
                    bin: bin
                })

                console.log("📥 RESPUESTA:", res)

                if (!res || res.ok === false) {
                    throw new Error(res?.error || "Error backend")
                }

                this.showOk(res.data || "Impresión OK")

                // opcional: limpiar input
                this.input.value = ""
                this.input.focus()

            } catch (err) {
                console.error("❌ ERROR:", err)
                this.showError(err.message)
            }
        }

        showOk(text) {
            this.msg.textContent = text
            this.msg.style.color = "green"
        }

        showError(text) {
            this.msg.textContent = text
            this.msg.style.color = "red"
        }

        destroy() {
            console.log("🧹 Destroy BinsPrintController")
        }
    }

    window.BinsPrintController = BinsPrintController
}