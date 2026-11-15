window.recursoRevisionPdf = (() => {
    const visores = new Map();

    function cerrar(id) {
        const visor = visores.get(id);
        if (visor) {
            visor.close();
            visores.delete(id);
        }
    }

    return {
        abrirVisor(id) {
            const visor = window.open("", "_blank");
            if (!visor) return false;
            visores.set(id, visor);
            return true;
        },
        mostrar(id, bytes) {
            const visor = visores.get(id);
            if (!visor || visor.closed) {
                visores.delete(id);
                throw new Error("La pestaña del documento fue cerrada.");
            }

            const url = URL.createObjectURL(new Blob([bytes], { type: "application/pdf" }));
            try {
                visor.location.replace(url);
            } catch (error) {
                URL.revokeObjectURL(url);
                throw error;
            }
            visores.delete(id);
            const timer = setInterval(() => {
                if (visor.closed) {
                    clearInterval(timer);
                    URL.revokeObjectURL(url);
                }
            }, 1000);
        },
        cerrar,
        descargar(bytes, nombreArchivo) {
            const url = URL.createObjectURL(new Blob([bytes], { type: "application/pdf" }));
            const enlace = document.createElement("a");
            enlace.href = url;
            enlace.download = nombreArchivo;
            enlace.click();
            setTimeout(() => URL.revokeObjectURL(url), 60000);
        }
    };
})();
