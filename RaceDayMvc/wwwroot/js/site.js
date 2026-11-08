// Fade out success alerts after a few seconds (errors and warnings stay until dismissed)
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.alert-success.rd-alert').forEach(function (alertEl) {
        setTimeout(function () {
            if (window.bootstrap && alertEl.isConnected) {
                bootstrap.Alert.getOrCreateInstance(alertEl).close();
            }
        }, 6000);
    });
});
