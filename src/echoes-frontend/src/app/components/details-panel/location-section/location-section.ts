import { Component, input } from '@angular/core';
import { Location } from '../../../models/world-context.model';

@Component({
  selector: 'app-location-section',
  standalone: true,
  templateUrl: './location-section.html'
})
export class LocationSectionComponent {
  location = input.required<Location>();
  isUpdating = input(false);

  formatCoords(x: number, y: number): string {
    return `(${x >= 0 ? '+' : ''}${x}, ${y >= 0 ? '+' : ''}${y})`;
  }
}
