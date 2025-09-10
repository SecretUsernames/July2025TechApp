// wwwroot/js/medvault-charts.js
(function () {
  const safe = (id) => document.getElementById(id);

  const baseOpts = {
    responsive: true,
    maintainAspectRatio: false,        // IMPORTANT: obey the wrapper height
    plugins: { legend: { display: false }, tooltip: { intersect: false, mode: 'index' } },
    scales: {
      x: { display: false, grid: { display: false } },
      y: { display: false, grid: { display: false } }
    },
    elements: { line: { tension: 0.35, borderWidth: 2 }, point: { radius: 0 } }
  };

  function makeChart(id, cfg) {
    const el = safe(id);
    if (!el) return;
    const ctx = el.getContext('2d');
    return new Chart(ctx, { type: 'line', data: cfg.data, options: { ...baseOpts, ...cfg.options } });
  }

  // Sample data (7 days)
  const labels = ['Mon','Tue','Wed','Thu','Fri','Sat','Sun'];

  window.medvaultCharts = {
    initAll: function () {
      // BP (two datasets, systolic/diastolic)
      makeChart('bpChart', {
        data: {
          labels,
          datasets: [
            { label:'Sys', data:[118,122,120,119,121,117,118], borderColor:'#2563eb', backgroundColor:'rgba(37,99,235,.15)', fill:true },
            { label:'Dia', data:[76,78,77,75,79,74,76], borderColor:'#06b6d4', backgroundColor:'rgba(6,182,212,.12)', fill:true }
          ]
        }
      });

      // Glucose
      makeChart('glucoseChart', {
        data: {
          labels,
          datasets: [
            { label:'Glucose', data:[126,129,118,132,128,124,127], borderColor:'#8b5cf6', backgroundColor:'rgba(139,92,246,.12)', fill:true }
          ]
        }
      });

      // Weight
      makeChart('weightChart', {
        data: {
          labels,
          datasets: [
            { label:'Weight', data:[163.2,163.0,162.8,162.7,162.6,162.5,162.4], borderColor:'#059669', backgroundColor:'rgba(5,150,105,.12)', fill:true }
          ]
        }
      });
    }
  };
})();
