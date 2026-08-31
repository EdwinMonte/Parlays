/**
 * ODDSPULSE PRO - REAL-TIME BETTING TERMINAL JAVASCRIPT
 * Real-time SignalR integration, Chart.js line movement tracker, Parlay builder
 */

// Application State
const appState = {
    currentFormat: localStorage.getItem('oddsFormat') || 'american',
    selectedCategory: 'All',
    selectedStatus: 'all',
    onlySharp: false,
    searchQuery: '',
    soundEnabled: localStorage.getItem('soundEnabled') === 'true',
    parlayLegs: [],
    stake: 100,
    activeChartModal: {
        matchId: null,
        optionId: null,
        chartInstance: null
    }
};

// Web Audio API Synthesizer for high-tech micro-audio feedback
let audioCtx = null;
function playTickSound(type = 'up') {
    if (!appState.soundEnabled) return;
    try {
        if (!audioCtx) {
            audioCtx = new (window.AudioContext || window.webkitAudioContext)();
        }
        if (audioCtx.state === 'suspended') {
            audioCtx.resume();
        }
        const osc = audioCtx.createOscillator();
        const gain = audioCtx.createGain();
        osc.type = 'sine';
        
        if (type === 'up') {
            osc.frequency.setValueAtTime(587.33, audioCtx.currentTime); // D5
            osc.frequency.exponentialRampToValueAtTime(880, audioCtx.currentTime + 0.08); // A5
        } else {
            osc.frequency.setValueAtTime(783.99, audioCtx.currentTime); // G5
            osc.frequency.exponentialRampToValueAtTime(440, audioCtx.currentTime + 0.08); // A4
        }
        
        gain.gain.setValueAtTime(0.04, audioCtx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.001, audioCtx.currentTime + 0.08);
        
        osc.connect(gain);
        gain.connect(audioCtx.destination);
        
        osc.start();
        osc.stop(audioCtx.currentTime + 0.08);
    } catch (e) {
        console.warn('Audio Context error', e);
    }
}

// Format Odds according to selected format
function formatOddsValue(american, decimal, probability) {
    if (appState.currentFormat === 'decimal') {
        return decimal.toFixed(2);
    } else if (appState.currentFormat === 'probability') {
        const prob = probability || (american > 0 ? (100 / (american + 100)) * 100 : (Math.abs(american) / (Math.abs(american) + 100)) * 100);
        return prob.toFixed(1) + '%';
    } else {
        return american > 0 ? `+${american}` : `${american}`;
    }
}

// Change active odds format
function setOddsFormat(format) {
    appState.currentFormat = format;
    localStorage.setItem('oddsFormat', format);

    document.querySelectorAll('.format-switcher button').forEach(b => b.classList.remove('active'));
    if (format === 'american') document.getElementById('btnFmtAmerican')?.classList.add('active');
    if (format === 'decimal') document.getElementById('btnFmtDecimal')?.classList.add('active');
    if (format === 'probability') document.getElementById('btnFmtProbability')?.classList.add('active');

    // Update all visible odds buttons on screen
    document.querySelectorAll('.odds-btn').forEach(btn => {
        const am = parseInt(btn.dataset.american, 10);
        const dec = parseFloat(btn.dataset.decimal);
        const prob = parseFloat(btn.dataset.probability);
        const valEl = btn.querySelector('.odds-val');
        if (valEl && !isNaN(am)) {
            valEl.textContent = formatOddsValue(am, dec, prob);
        }
    });

    // Update parlay summary
    renderParlaySummary();
}

// Toggle Audio Sound
function toggleAudio() {
    appState.soundEnabled = !appState.soundEnabled;
    localStorage.setItem('soundEnabled', appState.soundEnabled);
    updateAudioButtonUI();
    if (appState.soundEnabled) playTickSound('up');
}

function updateAudioButtonUI() {
    const icon = document.getElementById('soundIcon');
    const btn = document.getElementById('btnSoundToggle');
    if (!icon || !btn) return;
    if (appState.soundEnabled) {
        icon.className = 'fa-solid fa-volume-high text-success';
        btn.classList.add('border-success');
    } else {
        icon.className = 'fa-solid fa-volume-xmark text-muted';
        btn.classList.remove('border-success');
    }
}

// Filter matches by sport category
function filterCategory(category, el) {
    appState.selectedCategory = category;
    document.querySelectorAll('.nav-pills .nav-link').forEach(l => l.classList.remove('active'));
    if (el) el.classList.add('active');
    applyMatchFilters();
}

// Filter by status (Live, Upcoming, All)
function filterStatus(status, el) {
    appState.selectedStatus = status;
    document.querySelectorAll('.btn-filter-status').forEach(b => b.classList.remove('active'));
    if (el) el.classList.add('active');
    applyMatchFilters();
}

// Filter by Sharp Money Only
function toggleSharpOnly(checkbox) {
    appState.onlySharp = checkbox.checked;
    applyMatchFilters();
}

// Search filter
function onSearchMatches(query) {
    appState.searchQuery = query.toLowerCase().trim();
    applyMatchFilters();
}

// Apply composite filters to match cards
function applyMatchFilters() {
    const cards = document.querySelectorAll('.match-card-wrapper');
    let visibleCount = 0;

    cards.forEach(wrapper => {
        const sport = wrapper.dataset.sport || '';
        const status = wrapper.dataset.status || '';
        const isSharp = wrapper.dataset.sharp === 'true';
        const title = (wrapper.dataset.title || '').toLowerCase();

        let show = true;

        if (appState.selectedCategory !== 'All' && sport.toLowerCase() !== appState.selectedCategory.toLowerCase()) {
            show = false;
        }

        if (appState.selectedStatus !== 'all' && status.toLowerCase() !== appState.selectedStatus.toLowerCase()) {
            show = false;
        }

        if (appState.onlySharp && !isSharp) {
            show = false;
        }

        if (appState.searchQuery && !title.includes(appState.searchQuery)) {
            show = false;
        }

        wrapper.style.display = show ? 'block' : 'none';
        if (show) visibleCount++;
    });

    const emptyMsg = document.getElementById('noMatchesMessage');
    if (emptyMsg) {
        emptyMsg.style.display = visibleCount === 0 ? 'block' : 'none';
    }
}

// =========================================================
// PARLAY BUILDER LOGIC
// =========================================================
function toggleParlaySelection(matchId, matchTitle, marketType, selectionId, selectionName, american, decimal) {
    const existingIndex = appState.parlayLegs.findIndex(l => l.selectionId === selectionId);

    if (existingIndex >= 0) {
        // Remove leg
        appState.parlayLegs.splice(existingIndex, 1);
        updateBtnSelectedState(selectionId, false);
    } else {
        // Remove other selections from same market if exists to avoid conflict
        const sameMarketIndex = appState.parlayLegs.findIndex(l => l.matchId === matchId && l.marketType === marketType);
        if (sameMarketIndex >= 0) {
            const oldId = appState.parlayLegs[sameMarketIndex].selectionId;
            appState.parlayLegs.splice(sameMarketIndex, 1);
            updateBtnSelectedState(oldId, false);
        }

        // Add new leg
        appState.parlayLegs.push({
            matchId,
            matchTitle,
            marketType,
            selectionId,
            selectionName,
            americanOdds: parseInt(american, 10),
            decimalOdds: parseFloat(decimal)
        });
        updateBtnSelectedState(selectionId, true);
        playTickSound('up');
    }

    renderParlayLegsUI();
    fetchParlayCalculation();
}

function removeParlayLeg(selectionId) {
    const idx = appState.parlayLegs.findIndex(l => l.selectionId === selectionId);
    if (idx >= 0) {
        appState.parlayLegs.splice(idx, 1);
        updateBtnSelectedState(selectionId, false);
        renderParlayLegsUI();
        fetchParlayCalculation();
    }
}

function clearAllParlay() {
    appState.parlayLegs.forEach(leg => {
        updateBtnSelectedState(leg.selectionId, false);
    });
    appState.parlayLegs = [];
    renderParlayLegsUI();
    fetchParlayCalculation();
}

function updateBtnSelectedState(selectionId, isSelected) {
    const btn = document.querySelector(`.odds-btn[data-option-id="${selectionId}"]`);
    if (btn) {
        if (isSelected) {
            btn.classList.add('selected');
        } else {
            btn.classList.remove('selected');
        }
    }
}

function setQuickStake(val) {
    appState.stake = val;
    const input = document.getElementById('parlayStakeInput');
    if (input) input.value = val;
    fetchParlayCalculation();
}

function onStakeChanged(val) {
    const num = parseFloat(val);
    appState.stake = isNaN(num) || num <= 0 ? 100 : num;
    fetchParlayCalculation();
}

function renderParlayLegsUI() {
    const listEl = document.getElementById('parlayLegsList');
    const countEl = document.getElementById('parlayLegsCount');
    const emptyEl = document.getElementById('parlayEmptyState');
    const summaryEl = document.getElementById('parlaySummarySection');

    if (!listEl) return;

    if (countEl) countEl.textContent = `${appState.parlayLegs.length} ${appState.parlayLegs.length === 1 ? 'Selección' : 'Picks'}`;

    if (appState.parlayLegs.length === 0) {
        listEl.innerHTML = '';
        if (emptyEl) emptyEl.style.display = 'block';
        if (summaryEl) summaryEl.style.display = 'none';
        return;
    }

    if (emptyEl) emptyEl.style.display = 'none';
    if (summaryEl) summaryEl.style.display = 'block';

    listEl.innerHTML = appState.parlayLegs.map(leg => `
        <div class="parlay-leg-item">
            <button class="leg-remove-btn" onclick="removeParlayLeg('${leg.selectionId}')" title="Eliminar selección">
                <i class="fa-solid fa-xmark"></i>
            </button>
            <div class="leg-match-title"><i class="fa-solid fa-gamepad me-1 text-secondary"></i>${leg.matchTitle}</div>
            <div class="d-flex justify-content-between align-items-center mt-1">
                <span class="leg-selection">${leg.selectionName}</span>
                <span class="leg-odds">${formatOddsValue(leg.americanOdds, leg.decimalOdds)}</span>
            </div>
        </div>
    `).join('');
}

// Calculate Parlay with C# API
async function fetchParlayCalculation() {
    if (appState.parlayLegs.length === 0) return;

    try {
        const payload = {
            stake: appState.stake,
            legs: appState.parlayLegs
        };

        const res = await fetch('/api/oddsapi/calculate-parlay', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (!res.ok) throw new Error('Error calculando parlay');
        const data = await res.json();
        renderParlayCalculatedResult(data);
    } catch (err) {
        console.error('Error calculando parlay:', err);
    }
}

function renderParlayCalculatedResult(data) {
    const combinedOddsEl = document.getElementById('parlayCombinedOdds');
    const bonusBoostEl = document.getElementById('parlayBonusBoost');
    const finalPayoutEl = document.getElementById('parlayFinalPayout');
    const profitEl = document.getElementById('parlayProfit');
    const probEl = document.getElementById('parlayImpliedProb');
    const hedgeEl = document.getElementById('parlayHedgeAdvice');

    if (combinedOddsEl) combinedOddsEl.textContent = formatOddsValue(data.combinedAmericanOdds, data.combinedDecimalOdds);
    if (bonusBoostEl) {
        if (data.bonusPercentage > 0) {
            bonusBoostEl.style.display = 'inline-block';
            bonusBoostEl.textContent = `+${data.bonusPercentage}% BOOST`;
        } else {
            bonusBoostEl.style.display = 'none';
        }
    }
    if (finalPayoutEl) finalPayoutEl.textContent = `$${data.finalPayout.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
    if (profitEl) profitEl.textContent = `+$${data.finalProfit.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
    if (probEl) probEl.textContent = `${data.impliedWinProbability}%`;
    if (hedgeEl) hedgeEl.textContent = data.hedgeRecommendation;
}

// Simulate Bet Slip Placement Action
function placeSimulatedBet() {
    if (appState.parlayLegs.length === 0) {
        alert('Agrega al menos una selección para armar tu Parlay.');
        return;
    }
    const modalEl = document.getElementById('betSuccessModal');
    if (modalEl) {
        const bsModal = new bootstrap.Modal(modalEl);
        bsModal.show();
        clearAllParlay();
    }
}

// =========================================================
// LINE MOVEMENT CHART & BOOKMAKER MATRIX MODAL
// =========================================================
async function openMatchChartModal(matchId, defaultOptionId = null) {
    try {
        const res = await fetch(`/api/oddsapi/match/${matchId}`);
        if (!res.ok) throw new Error('No se pudo cargar el partido');
        const match = await res.json();

        appState.activeChartModal.matchId = matchId;

        // Populate Modal Header
        document.getElementById('modalMatchTitle').textContent = `${match.homeTeam} vs ${match.awayTeam}`;
        document.getElementById('modalMatchLeague').textContent = `${match.league} • ${match.sportName}`;
        document.getElementById('modalPublicTicketsHome').textContent = `${match.publicTicketsHomePercent}%`;
        document.getElementById('modalPublicTicketsAway').textContent = `${match.publicTicketsAwayPercent}%`;
        document.getElementById('modalMoneyHandleHome').textContent = `${match.moneyHandleHomePercent}%`;
        document.getElementById('modalMoneyHandleAway').textContent = `${match.moneyHandleAwayPercent}%`;

        // Render Selector of Options
        const optSelect = document.getElementById('modalOptionSelect');
        optSelect.innerHTML = '';

        let targetOption = null;
        match.markets.forEach(m => {
            m.options.forEach(opt => {
                const optEl = document.createElement('option');
                optEl.value = opt.id;
                optEl.textContent = `${m.displayName}: ${opt.name} (${opt.currentAmerican > 0 ? '+' : ''}${opt.currentAmerican})`;
                optSelect.appendChild(optEl);
                if (!targetOption || opt.id === defaultOptionId) {
                    targetOption = opt;
                }
            });
        });

        if (defaultOptionId) {
            optSelect.value = defaultOptionId;
        }

        // Render Sportsbooks Table
        renderBookmakersTable(match.sportsbooks);

        // Render Line Movement Chart
        await loadOptionHistoryChart(matchId, targetOption.id, targetOption.name);

        const modalEl = document.getElementById('chartDetailModal');
        const bsModal = new bootstrap.Modal(modalEl);
        bsModal.show();
    } catch (e) {
        console.error('Error al abrir modal:', e);
    }
}

function onModalOptionChanged() {
    const select = document.getElementById('modalOptionSelect');
    const optionId = select.value;
    const optionName = select.options[select.selectedIndex].text.split(':')[1]?.trim() || '';
    if (appState.activeChartModal.matchId && optionId) {
        loadOptionHistoryChart(appState.activeChartModal.matchId, optionId, optionName);
    }
}

async function loadOptionHistoryChart(matchId, optionId, optionName) {
    appState.activeChartModal.optionId = optionId;
    try {
        const res = await fetch(`/api/oddsapi/history?matchId=${matchId}&optionId=${optionId}`);
        const ticks = await res.json();

        renderHistoryChart(ticks, optionName);
    } catch (e) {
        console.error('Error cargando historial de ticks:', e);
    }
}

function renderHistoryChart(ticks, labelName) {
    const canvas = document.getElementById('lineMovementChart');
    if (!canvas) return;
    const ctx = canvas.getContext('2d');

    if (appState.activeChartModal.chartInstance) {
        appState.activeChartModal.chartInstance.destroy();
    }

    const labels = ticks.map(t => {
        const d = new Date(t.timestamp);
        return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    });

    const dataPoints = ticks.map(t => t.americanOdds);

    // Gradient background under the curve
    const gradient = ctx.createLinearGradient(0, 0, 0, 300);
    const isUnderdog = dataPoints[dataPoints.length - 1] > 0;
    
    if (isUnderdog) {
        gradient.addColorStop(0, 'rgba(0, 245, 155, 0.35)');
        gradient.addColorStop(1, 'rgba(0, 245, 155, 0.0)');
    } else {
        gradient.addColorStop(0, 'rgba(0, 210, 255, 0.35)');
        gradient.addColorStop(1, 'rgba(0, 210, 255, 0.0)');
    }

    const lineColor = isUnderdog ? '#00f59b' : '#00d2ff';

    appState.activeChartModal.chartInstance = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: `Evolución del Momio: ${labelName}`,
                data: dataPoints,
                borderColor: lineColor,
                backgroundColor: gradient,
                fill: true,
                tension: 0.35,
                borderWidth: 3,
                pointBackgroundColor: lineColor,
                pointBorderColor: '#0a0f18',
                pointBorderWidth: 2,
                pointRadius: 4,
                pointHoverRadius: 7
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                intersect: false,
                mode: 'index'
            },
            plugins: {
                legend: {
                    labels: {
                        color: '#94a3b8',
                        font: { family: 'Outfit', size: 13, weight: 'bold' }
                    }
                },
                tooltip: {
                    backgroundColor: '#0c1421',
                    titleColor: '#00d2ff',
                    bodyColor: '#fff',
                    borderColor: '#2d4166',
                    borderWidth: 1,
                    padding: 12,
                    callbacks: {
                        label: function (ctx) {
                            const val = ctx.parsed.y;
                            return ` Momio: ${val > 0 ? '+' : ''}${val} (Americano)`;
                        }
                    }
                }
            },
            scales: {
                x: {
                    grid: { color: 'rgba(255, 255, 255, 0.05)' },
                    ticks: { color: '#64748b', font: { family: 'JetBrains Mono', size: 11 } }
                },
                y: {
                    grid: { color: 'rgba(255, 255, 255, 0.06)' },
                    ticks: {
                        color: '#94a3b8',
                        font: { family: 'JetBrains Mono', size: 11 },
                        callback: function (val) {
                            return (val > 0 ? '+' : '') + val;
                        }
                    }
                }
            }
        }
    });
}

function renderBookmakersTable(sportsbooks) {
    const tbody = document.getElementById('bookmakerQuoteTableBody');
    if (!tbody) return;

    tbody.innerHTML = (sportsbooks || []).map(b => `
        <tr>
            <td>
                <div class="d-flex align-items-center gap-2">
                    <span class="badge" style="background:${b.badgeColor}; width:10px; height:10px; border-radius:50%;"></span>
                    <strong>${b.bookmakerName}</strong>
                </div>
            </td>
            <td>
                <span class="${b.isBestHome ? 'best-odds-badge' : 'text-white'}">${b.homeAmerican}</span>
            </td>
            <td>
                <span class="${b.isBestDraw ? 'best-odds-badge' : 'text-secondary'}">${b.drawAmerican || '-'}</span>
            </td>
            <td>
                <span class="${b.isBestAway ? 'best-odds-badge' : 'text-white'}">${b.awayAmerican}</span>
            </td>
            <td>
                <span class="badge bg-secondary-subtle text-info">${b.payoutRate}%</span>
            </td>
        </tr>
    `).join('');
}

// =========================================================
// SIGNALR REAL-TIME WEBSOCKET CONNECTION
// =========================================================
function initSignalRConnection() {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/oddshub")
        .withAutomaticReconnect([0, 2000, 5000, 10000])
        .build();

    // 1. Receive Odds Update
    connection.on("ReceiveOddsUpdate", function (data) {
        // data: { matchId, marketId, optionId, optionName, americanOdds, decimalOdds, impliedProbability, shiftPercentage, trend, timestamp }
        const btn = document.querySelector(`.odds-btn[data-option-id="${data.optionId}"]`);
        if (btn) {
            // Update data attributes
            btn.dataset.american = data.americanOdds;
            btn.dataset.decimal = data.decimalOdds;
            btn.dataset.probability = data.impliedProbability;

            // Update visible value
            const valEl = btn.querySelector('.odds-val');
            if (valEl) {
                valEl.textContent = formatOddsValue(data.americanOdds, data.decimalOdds, data.impliedProbability);
            }

            // Update shift badge
            const shiftBadge = btn.querySelector('.odds-shift-badge');
            if (shiftBadge) {
                if (data.shiftPercentage > 0) {
                    shiftBadge.className = 'odds-shift-badge shift-up';
                    shiftBadge.textContent = `▲ +${data.shiftPercentage}%`;
                } else if (data.shiftPercentage < 0) {
                    shiftBadge.className = 'odds-shift-badge shift-down';
                    shiftBadge.textContent = `▼ ${data.shiftPercentage}%`;
                } else {
                    shiftBadge.className = 'odds-shift-badge shift-none';
                    shiftBadge.textContent = `0.0%`;
                }
            }

            // Flash effect and audio
            btn.classList.remove('flash-up', 'flash-down');
            void btn.offsetWidth; // trigger reflow
            if (data.trend === 1) {
                btn.classList.add('flash-up');
                playTickSound('up');
            } else if (data.trend === -1) {
                btn.classList.add('flash-down');
                playTickSound('down');
            }
        }

        // If this option is currently in the active Parlay slip, update it dynamically
        const parlayLeg = appState.parlayLegs.find(l => l.selectionId === data.optionId);
        if (parlayLeg) {
            parlayLeg.americanOdds = data.americanOdds;
            parlayLeg.decimalOdds = data.decimalOdds;
            renderParlayLegsUI();
            fetchParlayCalculation();
        }

        // If modal chart is active for this option, append tick
        if (appState.activeChartModal.chartInstance && appState.activeChartModal.optionId === data.optionId) {
            const chart = appState.activeChartModal.chartInstance;
            chart.data.labels.push(data.timestamp);
            chart.data.datasets[0].data.push(data.americanOdds);
            if (chart.data.labels.length > 25) {
                chart.data.labels.shift();
                chart.data.datasets[0].data.shift();
            }
            chart.update('none');
        }
    });

    // 2. Receive Sharp Money Alert (RLM / Steam Move)
    connection.on("ReceiveSharpAlert", function (alert) {
        showSharpAlertToast(alert);
    });

    // 3. Receive Live Score Update
    connection.on("ReceiveScoreUpdate", function (scoreData) {
        const matchWrapper = document.querySelector(`.match-card-wrapper[data-match-id="${scoreData.matchId}"]`);
        if (matchWrapper) {
            const scoreEl = matchWrapper.querySelector('.score-display');
            if (scoreEl) scoreEl.textContent = `${scoreData.homeScore} - ${scoreData.awayScore}`;

            const timeBadge = matchWrapper.querySelector('.live-badge');
            if (timeBadge) timeBadge.innerHTML = `<span class="pulse-dot"></span> EN VIVO ${scoreData.liveTime}`;
        }
    });

    connection.start().then(() => {
        console.log("🟢 Conexión SignalR establecida con éxito.");
    }).catch(err => {
        console.warn("Error conectando a SignalR Hub:", err);
    });
}

function showSharpAlertToast(alert) {
    const toastContainer = document.getElementById('sharpAlertToastContainer');
    if (!toastContainer) return;

    const toastId = 'toast-' + Math.random().toString(36).substring(2, 9);
    const toastHtml = `
        <div id="${toastId}" class="toast align-items-center text-white bg-dark border-warning mb-2" role="alert" aria-live="assertive" aria-atomic="true" data-bs-delay="7000">
            <div class="d-flex">
                <div class="toast-body">
                    <div class="d-flex align-items-center gap-2 mb-1">
                        <span class="badge bg-warning text-dark fw-bold">${alert.title}</span>
                        <small class="text-muted">${alert.sport}</small>
                    </div>
                    <strong class="text-white">${alert.matchTitle}</strong>
                    <div class="small text-secondary mt-1">${alert.description}</div>
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;

    toastContainer.insertAdjacentHTML('beforeend', toastHtml);
    const toastEl = document.getElementById(toastId);
    const bsToast = new bootstrap.Toast(toastEl);
    bsToast.show();
}

// =========================================================
// +EV AND SUREBET / ARBITRAGE CALCULATOR LOGIC
// =========================================================
function calculateExpectedValue() {
    const oddsDec = parseFloat(document.getElementById('evBookmakerOdds').value) || 2.00;
    const fairProb = (parseFloat(document.getElementById('evFairProb').value) || 52) / 100;
    const stake = parseFloat(document.getElementById('evStake').value) || 100;

    // EV = (Fair Win Prob * Net Profit) - (Fair Loss Prob * Stake)
    const netProfit = stake * (oddsDec - 1);
    const evAmount = (fairProb * netProfit) - ((1 - fairProb) * stake);
    const evPercent = (evAmount / stake) * 100;

    const resultEl = document.getElementById('evResultBox');
    const valEl = document.getElementById('evCalculatedVal');
    const descEl = document.getElementById('evCalculatedDesc');

    if (resultEl && valEl && descEl) {
        valEl.textContent = `${evPercent >= 0 ? '+' : ''}${evPercent.toFixed(2)}% ($${evAmount.toFixed(2)})`;
        if (evPercent > 0) {
            valEl.className = 'text-success fw-bold fs-4';
            descEl.textContent = '¡Apuesta de Valor Positivo (+EV)! A largo plazo supera el margen de la casa de apuestas.';
        } else {
            valEl.className = 'text-danger fw-bold fs-4';
            descEl.textContent = 'Valor Negativo (-EV). La casa tiene ventaja matemática.';
        }
    }
}

function calculateArbitrage() {
    const odds1 = parseFloat(document.getElementById('arbOdds1').value) || 2.10;
    const odds2 = parseFloat(document.getElementById('arbOdds2').value) || 2.05;
    const totalBank = parseFloat(document.getElementById('arbTotalBank').value) || 1000;

    const inv1 = 1 / odds1;
    const inv2 = 1 / odds2;
    const totalInv = inv1 + inv2;

    const profitMargin = ((1 - totalInv) / totalInv) * 100;
    const stake1 = (totalBank * inv1) / totalInv;
    const stake2 = (totalBank * inv2) / totalInv;
    const guaranteedReturn = stake1 * odds1;
    const netProfit = guaranteedReturn - totalBank;

    const arbResultVal = document.getElementById('arbResultVal');
    const arbStake1 = document.getElementById('arbStake1');
    const arbStake2 = document.getElementById('arbStake2');
    const arbNetProfit = document.getElementById('arbNetProfit');

    if (arbResultVal) {
        if (profitMargin > 0) {
            arbResultVal.className = 'text-success fw-bold fs-4';
            arbResultVal.textContent = `¡Oportunidad de Arbitraje! Ganancia: +${profitMargin.toFixed(2)}%`;
        } else {
            arbResultVal.className = 'text-danger fw-bold fs-4';
            arbResultVal.textContent = `Sin Arbitraje (Margen Casa: ${Math.abs(profitMargin).toFixed(2)}%)`;
        }
    }
    if (arbStake1) arbStake1.textContent = `$${stake1.toFixed(2)}`;
    if (arbStake2) arbStake2.textContent = `$${stake2.toFixed(2)}`;
    if (arbNetProfit) arbNetProfit.textContent = `$${netProfit.toFixed(2)}`;
}

// Initial Boot
document.addEventListener('DOMContentLoaded', () => {
    setOddsFormat(appState.currentFormat);
    updateAudioButtonUI();
    initSignalRConnection();
});
