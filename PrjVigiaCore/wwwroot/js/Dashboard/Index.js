// Reloj en tiempo real
function updateClock() {
    const now = new Date();
    document.getElementById('live-clock').textContent =
        now.toLocaleTimeString('es-ES', { hour12: false });
}
setInterval(updateClock, 1000);
updateClock();

// Gráfico de procesos
const processCtx = document.getElementById('processChart').getContext('2d');
new Chart(processCtx, {
    type: 'bar',
    data: {
        labels: ['Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb', 'Dom'],
        datasets: [
            {
                label: 'Exitosos',
                data: [120, 150, 110, 180, 200, 90, 70],
                backgroundColor: '#2ecc71'
            },
            {
                label: 'Fallidos',
                data: [5, 8, 12, 6, 3, 2, 1],
                backgroundColor: '#e74c3c'
            }
        ]
    },
    options: {
        responsive: true,
        scales: {
            y: { beginAtZero: true }
        }
    }
});