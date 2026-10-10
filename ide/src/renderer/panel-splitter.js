(function(root) {
  function bounds(extent, minimum, reserve, thickness, maximum = Infinity) {
    const max = Math.max(0, Math.min(maximum, extent - reserve - thickness));
    return { min: Math.min(minimum, max), max };
  }
  function clamp(value, limits) { return Math.max(limits.min, Math.min(Number.isFinite(value) ? value : limits.min, limits.max)); }
  class PanelSplitter {
    constructor({handle, panel, container, axis, minimum, reserve, maximum, defaultSize, savedSize, onCommit}) {
      Object.assign(this, {handle, panel, container, axis, minimum, reserve, maximum, defaultSize, onCommit});
      this.preferred = Number.isFinite(savedSize) && savedSize > 0 ? savedSize : defaultSize;
      this.drag = null; this.frame = null; this.lastExtent = this.extent();
      handle.tabIndex = 0; handle.setAttribute('role', 'separator');
      handle.setAttribute('aria-orientation', axis === 'x' ? 'vertical' : 'horizontal');
      handle.addEventListener('pointerdown', e => this.start(e));
      handle.addEventListener('pointermove', e => this.move(e));
      handle.addEventListener('pointerup', e => this.finish(e));
      handle.addEventListener('pointercancel', e => { if (this.drag?.id === e.pointerId) this.cancel(); });
      handle.addEventListener('lostpointercapture', e => { if (this.drag?.id === e.pointerId) this.cancel(); });
      handle.addEventListener('dblclick', e => { e.preventDefault(); this.reset(); });
      handle.addEventListener('keydown', e => this.key(e));
      window.addEventListener('blur', () => this.cancel());
      document.addEventListener('keydown', e => { if (e.key === 'Escape' && this.drag) { e.preventDefault(); this.cancel(); } });
      this.observer = new ResizeObserver(() => {
        const extent = this.extent(); if (extent === this.lastExtent) return;
        this.lastExtent = extent; this.cancel(); this.layout();
      });
      this.observer.observe(container); this.layout();
    }
    extent() { return this.axis === 'x' ? this.container.clientWidth : this.container.clientHeight; }
    limits() {
      const thickness = this.axis === 'x' ? this.handle.offsetWidth : this.handle.offsetHeight;
      return bounds(this.extent(), this.minimum, this.reserve, thickness, this.maximum);
    }
    current() { const rect = this.panel.getBoundingClientRect(); return this.axis === 'x' ? rect.width : rect.height; }
    coordinate(e) { return this.axis === 'x' ? e.clientX : e.clientY; }
    apply(value) {
      const limits = this.limits(); const size = Math.round(clamp(value, limits));
      this.panel.style[this.axis === 'x' ? 'width' : 'height'] = size + 'px';
      if (this.axis === 'x') this.panel.style.flexBasis = size + 'px';
      for (const [name, val] of [['min', limits.min], ['max', limits.max], ['now', size]]) this.handle.setAttribute('aria-value' + name, String(Math.round(val)));
    }
    layout() { if (!this.drag) this.apply(this.preferred); }
    start(e) {
      if (e.button !== 0 || !e.isPrimary || this.drag || this.handle.hidden) return;
      this.handle.focus({preventScroll: true});
      try { this.handle.setPointerCapture(e.pointerId); } catch { return; }
      this.drag = {id: e.pointerId, coordinate: this.coordinate(e), size: this.current(), value: this.current()};
      this.handle.classList.add('dragging'); document.body.classList.add('splitter-dragging', 'splitter-' + this.axis);
    }
    move(e) {
      if (!this.drag || this.drag.id !== e.pointerId) return;
      this.drag.value = this.drag.size + (this.coordinate(e) - this.drag.coordinate) * (this.axis === 'x' ? 1 : -1);
      if (this.frame === null) this.frame = requestAnimationFrame(() => { this.frame = null; if (this.drag) this.apply(this.drag.value); });
    }
    finish(e) {
      if (!this.drag || this.drag.id !== e.pointerId) return;
      this.move(e); const value = clamp(this.drag.value, this.limits()); this.cleanup();
      this.preferred = value; this.apply(value); this.onCommit(Math.round(value));
    }
    cleanup() {
      const id = this.drag?.id; this.drag = null;
      if (this.frame !== null) cancelAnimationFrame(this.frame); this.frame = null;
      this.handle.classList.remove('dragging'); document.body.classList.remove('splitter-dragging', 'splitter-x', 'splitter-y');
      if (id !== undefined && this.handle.hasPointerCapture(id)) this.handle.releasePointerCapture(id);
    }
    cancel() { if (!this.drag) return; this.cleanup(); this.apply(this.preferred); }
    reset() { this.cancel(); this.preferred = this.defaultSize; this.layout(); this.onCommit(null); }
    key(e) {
      if (e.key === 'Escape') { this.cancel(); return; }
      if (e.key === 'Enter') { e.preventDefault(); this.reset(); return; }
      const sign = this.axis === 'x' ? {ArrowLeft: -1, ArrowRight: 1}[e.key] : {ArrowUp: 1, ArrowDown: -1}[e.key];
      if (sign === undefined && !['Home', 'End'].includes(e.key)) return;
      e.preventDefault(); this.cancel(); const limits = this.limits();
      const value = e.key === 'Home' ? limits.min : e.key === 'End' ? limits.max : this.current() + sign * (e.shiftKey ? 40 : 10);
      this.preferred = clamp(value, limits); this.layout(); this.onCommit(Math.round(this.preferred));
    }
  }
  const api = {PanelSplitter, bounds, clamp};
  if (typeof module === 'object' && module.exports) module.exports = api;
  root.PanelSplitters = api;
})(typeof window === 'undefined' ? globalThis : window);
