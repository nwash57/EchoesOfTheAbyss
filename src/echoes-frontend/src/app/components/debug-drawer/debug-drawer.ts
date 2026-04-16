import { Component, input, computed } from '@angular/core';
import { AccordionSectionComponent } from '../details-panel/accordion-section/accordion-section';
import { DebugStateMessage } from '../../models/server-messages';

@Component({
  selector: 'app-debug-drawer',
  standalone: true,
  imports: [AccordionSectionComponent],
  templateUrl: './debug-drawer.html'
})
export class DebugDrawerComponent {
  debugState = input.required<DebugStateMessage | null>();

  plotArc = computed(() => this.debugState()?.plotArc ?? null);
  evaluation = computed(() => this.debugState()?.evaluation ?? null);
  tracker = computed(() => this.debugState()?.trackerState ?? null);
  plotAction = computed(() => this.debugState()?.plotAction ?? 'None');
  round = computed(() => this.debugState()?.round ?? 0);

  alignmentColor(alignment: string): string {
    switch (alignment) {
      case 'on_track': return 'oklch(65% 0.15 145)';
      case 'drifting': return 'oklch(70% 0.15 75)';
      case 'diverged': return 'oklch(55% 0.18 25)';
      default: return 'oklch(50% 0.02 265)';
    }
  }

  healthDeltaColor(delta: number): string {
    if (delta > 0) return 'oklch(65% 0.15 145)';
    if (delta < 0) return 'oklch(55% 0.18 25)';
    return 'oklch(50% 0.02 265)';
  }

  formatDelta(delta: number): string {
    return delta > 0 ? `+${delta}` : `${delta}`;
  }
}
