import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-placeholder-page',
  standalone: true,
  template: `
    <section class="page">
      <p class="kicker">NoticeSaaS</p>
      <h1>{{ title }}</h1>
      <p class="lede">This module is stubbed in the shell. Full workflows land in later days.</p>
    </section>
  `,
  styles: `
    .page {
      max-width: 36rem;
      position: relative;
    }
    .page::before {
      content: "";
      position: absolute;
      left: -1.25rem;
      top: 0.35rem;
      bottom: 0.35rem;
      width: 3px;
      background: var(--copper);
    }
    .kicker {
      margin: 0;
      font-family: var(--font-display);
      font-size: 1rem;
      font-weight: 600;
      color: var(--copper-deep);
    }
    h1 {
      margin: 0.55rem 0 0;
      font-family: var(--font-display);
      font-size: clamp(1.9rem, 3.5vw, 2.4rem);
      font-weight: 600;
      letter-spacing: -0.03em;
      color: var(--ink);
    }
    .lede {
      margin: 1rem 0 0;
      line-height: 1.6;
      color: var(--muted);
    }
  `
})
export class PlaceholderPageComponent {
  readonly title = inject(ActivatedRoute).snapshot.data['title'] as string;
}
