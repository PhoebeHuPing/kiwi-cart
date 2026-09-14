/*
 * KiwiCart — Woolworths store coordinate fetcher (browser console script).
 *
 * WHY: api.cdx.nz is behind Akamai bot protection and is only reachable from a
 * real browser that already holds the Akamai cookies. Our backend cannot reach
 * it. So we fetch the store details here (in your logged-in woolworths.co.nz
 * tab) and download a JSON file that KiwiCart imports once into the stores table.
 *
 * HOW TO RUN:
 *   1. Open https://www.woolworths.co.nz in Chrome and make sure the page loads
 *      (this gives your browser the Akamai cookies for cdx.nz).
 *   2. Open DevTools (F12) -> Console.
 *   3. Paste this entire file and press Enter.
 *   4. Wait for it to finish (~3-4 min with the delay). It downloads
 *      "ww-stores-full.json" automatically. Send that file back.
 *
 * It is polite: one request at a time with a delay, and it retries once on
 * failure. Adjust DELAY_MS up if you see 429/403 responses.
 */
(async () => {
  const SITE_NOS = [
    9114, 9435, 9124, 9183, 9489, 9168, 9033, 9538, 9115, 9427, 9146, 9130,
    9113, 9444, 9156, 9164, 9524, 9405, 9433, 9927, 9431, 9284, 9522, 9212,
    9013, 9702, 9040, 9095, 9293, 9502, 9202, 9424, 9221, 9249, 9151, 9501,
    9460, 9432, 9128, 9162, 9554, 9508, 9191, 9175, 9098, 9169, 9464, 9173,
    9103, 9449, 9578, 9197, 9282, 9159, 9530, 9094, 9133, 9102, 9546, 9031,
    9011, 9034, 9177, 9032, 9149, 9100, 9528, 9118, 9141, 9180, 9474, 9181,
    9147, 9597, 9518, 9190, 9048, 9472, 9050, 9469, 9061, 9090, 9161, 9140,
    9400, 9475, 9463, 9237, 9075, 9182, 9217, 9486, 9429, 9216, 9238, 9414,
    9584, 9123, 9458, 9533, 9448, 9064, 9478, 9049, 9160, 9038, 9185, 9535,
    9066, 9198, 9459, 9062, 9453, 9143, 9158, 9037, 9224, 9415, 9065, 9145,
    9193, 9465, 9131, 9265, 9069, 9430, 9192, 9170, 9119, 9204, 9171, 9428,
    9541, 9231, 9144, 9155, 9058, 9248, 9096, 9014, 9495, 9534, 9163, 9485,
    9174, 9063, 9529, 9477, 9452, 9194, 9547, 9195, 9213, 9092, 9442, 9091,
    9077, 9057, 9106, 9243, 9178, 9437, 9235, 9551, 9030, 9154, 9176, 9233,
    9153, 9292, 9521, 9199, 9290, 9504, 9129, 9283, 9107, 9121, 9120, 9157,
    9450, 9189, 9142, 9242, 9201, 9136, 9425, 9473, 9467, 9068, 9206,
  ];

  const BASE = 'https://api.cdx.nz/site-location/api/v2/sites/';
  const DELAY_MS = 800; // pause between requests; raise if throttled
  const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

  const results = [];
  const failures = [];

  async function fetchOne(siteNo, attempt = 1) {
    try {
      const resp = await fetch(BASE + siteNo, {
        headers: { accept: 'application/json' },
        credentials: 'include',
      });
      if (!resp.ok) throw new Error('HTTP ' + resp.status);
      const data = await resp.json();
      // The endpoint returns an array with one { site, tradingHours, ... }.
      const entry = Array.isArray(data) ? data[0] : data;
      const s = entry && entry.site ? entry.site : null;
      if (!s) throw new Error('no site in response');
      results.push({
        siteNo: s.id,
        name: s.name,
        division: s.division,
        addressLine1: s.addressLine1,
        suburb: s.suburb,
        postcode: s.postcode,
        latitude: s.latitude,
        longitude: s.longitude,
      });
    } catch (err) {
      if (attempt < 2) {
        await sleep(1500);
        return fetchOne(siteNo, attempt + 1);
      }
      failures.push({ siteNo, error: String(err) });
    }
  }

  console.log('Fetching ' + SITE_NOS.length + ' Woolworths sites from cdx.nz ...');
  for (let i = 0; i < SITE_NOS.length; i++) {
    await fetchOne(SITE_NOS[i]);
    if ((i + 1) % 20 === 0) console.log('  ' + (i + 1) + '/' + SITE_NOS.length);
    await sleep(DELAY_MS);
  }

  console.log('Done. Success: ' + results.length + ', Failed: ' + failures.length);
  if (failures.length) console.warn('Failures:', failures);

  // Trigger a download of the collected data.
  const blob = new Blob([JSON.stringify({ stores: results, failures }, null, 2)], {
    type: 'application/json',
  });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = 'ww-stores-full.json';
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
  console.log('Downloaded ww-stores-full.json — send this file back.');
})();
