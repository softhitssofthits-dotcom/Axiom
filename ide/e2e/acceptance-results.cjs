function expectedCancellation(error) {
  return /^Canceled: Canceled\s/.test(error) && error.includes('editorWorkerHost-');
}

function configurationOnly(row) {
  if (!['inertialScroll', 'mouseMiddleClickAction'].includes(row.key) || !row.limitation) return false;
  const values = row.key === 'inertialScroll' ? [false, true] : ['default', 'openLink', 'ctrlLeftClick'];
  return !row.errors.length && row.cases.length === values.length && values.every(value =>
    row.cases.some(item => item.value === value && item.diagnostic.parent === value)) &&
    (row.key !== 'mouseMiddleClickAction' || row.cases.every(item => item.observed.urls.length === 0));
}

function acceptedSetting(row) {
  return ['observed-change', 'unsupported', 'verified-equivalent'].includes(row.status) ||
    row.status === 'verified-configuration' && configurationOnly(row);
}

function summarize(results) {
  return {
    executed: results.length,
    failed: results.filter(row => row.failures.length || row.crashed || row.pageErrors.some(error => !expectedCancellation(error))).length,
    passedChecks: results.reduce((total, row) => total + row.passed, 0),
    limitations: results.flatMap(row => (row.limitations || []).map(item => ({ id: row.id, ...item }))),
    expectedCancellations: results.reduce((total, row) => total + row.pageErrors.filter(expectedCancellation).length, 0),
  };
}

module.exports = { expectedCancellation, configurationOnly, acceptedSetting, summarize };
