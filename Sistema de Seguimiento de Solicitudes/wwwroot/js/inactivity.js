let inactivityTimeout;
let inactivityDotnetHelper;

// Cambiar el numero para cerrar sesion automaticamente 
let logoutTimeInMinutes = 15;

function resetInactivityTimer() {
    clearTimeout(inactivityTimeout);
    inactivityTimeout = setTimeout(() => {
        inactivityDotnetHelper?.invokeMethodAsync("CerrarSesionAuto");
    }, logoutTimeInMinutes * 60 * 1000);
}

function startInactivityWatcher(dotnetHelper) {
    inactivityDotnetHelper = dotnetHelper;
    document.addEventListener("mousemove", resetInactivityTimer);
    document.addEventListener("keydown", resetInactivityTimer);
    document.addEventListener("click", resetInactivityTimer);
    resetInactivityTimer();
}

function stopInactivityWatcher() {
    clearTimeout(inactivityTimeout);
    document.removeEventListener("mousemove", resetInactivityTimer);
    document.removeEventListener("keydown", resetInactivityTimer);
    document.removeEventListener("click", resetInactivityTimer);
    inactivityDotnetHelper = null;
}
