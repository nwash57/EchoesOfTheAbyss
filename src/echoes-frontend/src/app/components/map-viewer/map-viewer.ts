import { Component, computed, inject, signal } from '@angular/core';
import { GameService } from '../../services/game.service';
import { Location } from '../../models/world-context.model';

@Component({
  selector: 'app-map-viewer',
  standalone: true,
  templateUrl: './map-viewer.html',
  styles: [`
    :host {
      display: block;
      height: 100%;
      width: 100%;
      overflow: hidden;
      position: relative;
    }
  `]
})
export class MapViewerComponent {
  protected game = inject(GameService);

  // Zoom level: meters per pixel or pixels per meter?
  // User says 1 unit = 10 meters.
  // Let's say zoom is "pixels per unit (10m)".
  // zoom = 20 means 1 unit (10m) = 20 pixels.
  protected zoom = signal(20);
  protected offset = signal({ x: 0, y: 0 });

  protected locations = this.game.knownLocations;
  protected currentLocation = computed(() => this.game.worldContext().currentLocation);

  protected hoveredLocation = signal<Location | null>(null);

  protected transform = computed(() => {
    const z = this.zoom();
    const off = this.offset();
    return `translate(${off.x}px, ${off.y}px) scale(1)`;
  });

  // Calculate position for a location
  protected getPos(loc: Location) {
    const z = this.zoom();
    // Centering: assume (0,0) is center of container initially?
    // For now just raw coordinates * zoom.
    return {
      left: `${loc.coordinates.x * z}px`,
      top: `${-loc.coordinates.y * z}px` // Y is up in many games, down in CSS
    };
  }

  protected zoomIn() {
    this.zoom.update(z => Math.min(z * 1.2, 100));
  }

  protected zoomOut() {
    this.zoom.update(z => Math.max(z / 1.2, 5));
  }

  protected centerOnPlayer() {
    const cur = this.currentLocation();
    if (cur) {
      // Need container dimensions for real centering, but for now just reset offset
      // relative to current loc.
      const z = this.zoom();
      this.offset.set({
        x: -cur.coordinates.x * z,
        y: cur.coordinates.y * z
      });
    }
  }

  private isDragging = false;
  private lastMousePos = { x: 0, y: 0 };

  protected onMouseDown(event: MouseEvent) {
    this.isDragging = true;
    this.lastMousePos = { x: event.clientX, y: event.clientY };
  }

  protected onMouseMove(event: MouseEvent) {
    if (this.isDragging) {
      const dx = event.clientX - this.lastMousePos.x;
      const dy = event.clientY - this.lastMousePos.y;
      this.offset.update(off => ({ x: off.x + dx, y: off.y + dy }));
      this.lastMousePos = { x: event.clientX, y: event.clientY };
    }
  }

  protected onMouseUp() {
    this.isDragging = false;
  }
}
