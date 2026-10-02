import { useEffect, useRef, type ReactNode } from 'react';
import { createPortal } from 'react-dom';
import { X } from 'lucide-react';
export function Overlay({ title, children, onClose, drawer = false }: { title: string; children: ReactNode; onClose: () => void; drawer?: boolean }) {
  const panel = useRef<HTMLDivElement>(null); const close = useRef(onClose);
  useEffect(() => { close.current = onClose; }, [onClose]);
  useEffect(() => {
    const previous = document.activeElement as HTMLElement | null; const overflow = document.body.style.overflow; document.body.style.overflow = 'hidden'; panel.current?.focus();
    const keydown = (e: KeyboardEvent) => { if (e.key === 'Escape') { e.preventDefault(); close.current(); } if (e.key === 'Tab') { const elements = [...(panel.current?.querySelectorAll<HTMLElement>('button:not(:disabled),input:not(:disabled),select:not(:disabled),textarea:not(:disabled),a[href],[tabindex="0"]') || [])]; const first = elements[0], last = elements.at(-1); if (!first) { e.preventDefault(); return; } if (e.shiftKey && (document.activeElement === first || document.activeElement === panel.current)) { e.preventDefault(); last?.focus(); } else if (!e.shiftKey && (document.activeElement === last || document.activeElement === panel.current)) { e.preventDefault(); first.focus(); } } };
    document.addEventListener('keydown', keydown); return () => { document.body.style.overflow = overflow; document.removeEventListener('keydown', keydown); previous?.focus(); };
  }, []);
  return createPortal(<div className={`overlay ${drawer ? 'overlay-drawer' : ''}`} onMouseDown={e => { if (e.target === e.currentTarget) onClose(); }}><div ref={panel} tabIndex={-1} role="dialog" aria-modal="true" aria-label={title} className={drawer ? 'drawer' : 'modal'}><header><h2>{title}</h2><button type="button" className="icon-button" aria-label="Fechar" onClick={onClose}><X size={22}/></button></header>{children}</div></div>, document.body);
}
