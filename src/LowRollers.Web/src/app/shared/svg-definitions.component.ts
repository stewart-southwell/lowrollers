import { Component, ChangeDetectionStrategy } from '@angular/core';

/**
 * Component that defines reusable SVG symbols for cards and chips.
 * Include this component once at the app root to make symbols available globally.
 * Use symbols via: <svg><use href="#symbolId" /></svg>
 */
@Component({
  selector: 'app-svg-definitions',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg aria-hidden="true" style="position: absolute; width: 0; height: 0; overflow: hidden" xmlns="http://www.w3.org/2000/svg">
      <defs>
        <!-- Ornate pattern for card backs -->
        <pattern id="ornatePattern" patternUnits="userSpaceOnUse" width="12" height="12">
          <rect width="12" height="12" fill="#1a5f2a" />
          <circle cx="6" cy="6" r="4" fill="none" stroke="#2d8a4a" stroke-width="0.5" />
          <circle cx="0" cy="0" r="2" fill="#2d8a4a" />
          <circle cx="12" cy="0" r="2" fill="#2d8a4a" />
          <circle cx="0" cy="12" r="2" fill="#2d8a4a" />
          <circle cx="12" cy="12" r="2" fill="#2d8a4a" />
          <path d="M6 0 L6 12 M0 6 L12 6" stroke="#2d8a4a" stroke-width="0.3" />
        </pattern>

        <!-- Card back symbol -->
        <symbol id="cardBack" viewBox="0 0 208 303">
          <rect width="208" height="303" rx="12" fill="#fff" />
          <rect x="4" y="4" width="200" height="295" rx="10" fill="#8B0000" />
          <rect x="8" y="8" width="192" height="287" rx="8" fill="#fff" />
          <rect x="12" y="12" width="184" height="279" rx="6" fill="#1a5f2a" />
          <rect x="12" y="12" width="184" height="279" rx="6" fill="url(#ornatePattern)" />
          <rect x="20" y="20" width="168" height="263" rx="4" fill="none" stroke="#c9a227" stroke-width="3" />
          <rect x="28" y="28" width="152" height="247" rx="3" fill="none" stroke="#c9a227" stroke-width="1" />
          <!-- Center diamond decoration -->
          <g transform="translate(104, 151.5)">
            <rect x="-30" y="-40" width="60" height="80" rx="4" fill="#8B0000" transform="rotate(45)" />
            <rect x="-22" y="-32" width="44" height="64" rx="3" fill="#c9a227" transform="rotate(45)" />
            <rect x="-16" y="-24" width="32" height="48" rx="2" fill="#8B0000" transform="rotate(45)" />
            <circle r="12" fill="#c9a227" />
            <circle r="8" fill="#8B0000" />
            <circle r="4" fill="#c9a227" />
          </g>
          <!-- Corner decorations -->
          <g fill="#c9a227">
            <circle cx="40" cy="40" r="6" />
            <circle cx="168" cy="40" r="6" />
            <circle cx="40" cy="263" r="6" />
            <circle cx="168" cy="263" r="6" />
          </g>
        </symbol>

        <!-- Chip gradients -->
        <radialGradient id="chipGradWhite" cx="30%" cy="30%">
          <stop offset="0%" stop-color="#ffffff" />
          <stop offset="100%" stop-color="#d1d5db" />
        </radialGradient>
        <radialGradient id="chipInnerGradWhite" cx="30%" cy="30%">
          <stop offset="0%" stop-color="#ffffff" />
          <stop offset="100%" stop-color="#e5e7eb" />
        </radialGradient>

        <radialGradient id="chipGradRed" cx="30%" cy="30%">
          <stop offset="0%" stop-color="#ef4444" />
          <stop offset="100%" stop-color="#991b1b" />
        </radialGradient>
        <radialGradient id="chipInnerGradRed" cx="30%" cy="30%">
          <stop offset="0%" stop-color="#f87171" />
          <stop offset="100%" stop-color="#b91c1c" />
        </radialGradient>

        <radialGradient id="chipGradBlue" cx="30%" cy="30%">
          <stop offset="0%" stop-color="#3b82f6" />
          <stop offset="100%" stop-color="#1e3a8a" />
        </radialGradient>
        <radialGradient id="chipInnerGradBlue" cx="30%" cy="30%">
          <stop offset="0%" stop-color="#60a5fa" />
          <stop offset="100%" stop-color="#1d4ed8" />
        </radialGradient>

        <radialGradient id="chipGradGreen" cx="30%" cy="30%">
          <stop offset="0%" stop-color="#22c55e" />
          <stop offset="100%" stop-color="#14532d" />
        </radialGradient>
        <radialGradient id="chipInnerGradGreen" cx="30%" cy="30%">
          <stop offset="0%" stop-color="#4ade80" />
          <stop offset="100%" stop-color="#15803d" />
        </radialGradient>

        <radialGradient id="chipGradBlack" cx="30%" cy="30%">
          <stop offset="0%" stop-color="#4b5563" />
          <stop offset="100%" stop-color="#111827" />
        </radialGradient>
        <radialGradient id="chipInnerGradBlack" cx="30%" cy="30%">
          <stop offset="0%" stop-color="#6b7280" />
          <stop offset="100%" stop-color="#1f2937" />
        </radialGradient>

        <!-- White chip symbol -->
        <symbol id="chipWhite" viewBox="0 0 100 100">
          <circle cx="50" cy="50" r="48" fill="#e5e7eb" />
          <circle cx="50" cy="50" r="48" fill="url(#chipGradWhite)" />
          <circle cx="50" cy="50" r="40" fill="none" stroke="#374151" stroke-width="3" stroke-dasharray="12 8" />
          <circle cx="50" cy="50" r="24" fill="#f3f4f6" />
          <circle cx="50" cy="50" r="24" fill="url(#chipInnerGradWhite)" />
          <circle cx="50" cy="50" r="18" fill="none" stroke="#374151" stroke-width="2" />
          <g fill="#374151">
            <rect x="47" y="2" width="6" height="12" rx="2" />
            <rect x="47" y="86" width="6" height="12" rx="2" />
            <rect x="2" y="47" width="12" height="6" rx="2" />
            <rect x="86" y="47" width="12" height="6" rx="2" />
          </g>
        </symbol>

        <!-- Red chip symbol -->
        <symbol id="chipRed" viewBox="0 0 100 100">
          <circle cx="50" cy="50" r="48" fill="#b91c1c" />
          <circle cx="50" cy="50" r="48" fill="url(#chipGradRed)" />
          <circle cx="50" cy="50" r="40" fill="none" stroke="#fff" stroke-width="3" stroke-dasharray="12 8" />
          <circle cx="50" cy="50" r="24" fill="#dc2626" />
          <circle cx="50" cy="50" r="24" fill="url(#chipInnerGradRed)" />
          <circle cx="50" cy="50" r="18" fill="none" stroke="#fff" stroke-width="2" />
          <g fill="#fff">
            <rect x="47" y="2" width="6" height="12" rx="2" />
            <rect x="47" y="86" width="6" height="12" rx="2" />
            <rect x="2" y="47" width="12" height="6" rx="2" />
            <rect x="86" y="47" width="12" height="6" rx="2" />
          </g>
        </symbol>

        <!-- Blue chip symbol -->
        <symbol id="chipBlue" viewBox="0 0 100 100">
          <circle cx="50" cy="50" r="48" fill="#1e40af" />
          <circle cx="50" cy="50" r="48" fill="url(#chipGradBlue)" />
          <circle cx="50" cy="50" r="40" fill="none" stroke="#fff" stroke-width="3" stroke-dasharray="12 8" />
          <circle cx="50" cy="50" r="24" fill="#2563eb" />
          <circle cx="50" cy="50" r="24" fill="url(#chipInnerGradBlue)" />
          <circle cx="50" cy="50" r="18" fill="none" stroke="#fff" stroke-width="2" />
          <g fill="#fff">
            <rect x="47" y="2" width="6" height="12" rx="2" />
            <rect x="47" y="86" width="6" height="12" rx="2" />
            <rect x="2" y="47" width="12" height="6" rx="2" />
            <rect x="86" y="47" width="12" height="6" rx="2" />
          </g>
        </symbol>

        <!-- Green chip symbol -->
        <symbol id="chipGreen" viewBox="0 0 100 100">
          <circle cx="50" cy="50" r="48" fill="#15803d" />
          <circle cx="50" cy="50" r="48" fill="url(#chipGradGreen)" />
          <circle cx="50" cy="50" r="40" fill="none" stroke="#fff" stroke-width="3" stroke-dasharray="12 8" />
          <circle cx="50" cy="50" r="24" fill="#22c55e" />
          <circle cx="50" cy="50" r="24" fill="url(#chipInnerGradGreen)" />
          <circle cx="50" cy="50" r="18" fill="none" stroke="#fff" stroke-width="2" />
          <g fill="#fff">
            <rect x="47" y="2" width="6" height="12" rx="2" />
            <rect x="47" y="86" width="6" height="12" rx="2" />
            <rect x="2" y="47" width="12" height="6" rx="2" />
            <rect x="86" y="47" width="12" height="6" rx="2" />
          </g>
        </symbol>

        <!-- Black chip symbol -->
        <symbol id="chipBlack" viewBox="0 0 100 100">
          <circle cx="50" cy="50" r="48" fill="#1f2937" />
          <circle cx="50" cy="50" r="48" fill="url(#chipGradBlack)" />
          <circle cx="50" cy="50" r="40" fill="none" stroke="#ffd700" stroke-width="3" stroke-dasharray="12 8" />
          <circle cx="50" cy="50" r="24" fill="#374151" />
          <circle cx="50" cy="50" r="24" fill="url(#chipInnerGradBlack)" />
          <circle cx="50" cy="50" r="18" fill="none" stroke="#ffd700" stroke-width="2" />
          <g fill="#ffd700">
            <rect x="47" y="2" width="6" height="12" rx="2" />
            <rect x="47" y="86" width="6" height="12" rx="2" />
            <rect x="2" y="47" width="12" height="6" rx="2" />
            <rect x="86" y="47" width="12" height="6" rx="2" />
          </g>
        </symbol>
      </defs>
    </svg>
  `,
  styles: []
})
export class SvgDefinitionsComponent {}
