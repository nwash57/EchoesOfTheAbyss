import { Component, inject, signal } from '@angular/core';
import { GameService } from '../../services/game.service';
import { EquipmentPiece, EquipmentSlot } from '../../models/world-context.model';
import { MapViewerComponent } from '../map-viewer/map-viewer';

interface SlotMeta {
  icon: string;
  label: string;
  piece: EquipmentPiece | null;
}

@Component({
  selector: 'app-details-panel',
  standalone: true,
  imports: [MapViewerComponent],
  templateUrl: './details-panel.html'
})
export class DetailsPanelComponent {
  protected game = inject(GameService);

  protected playerExpanded = signal(true);
  protected locationExpanded = signal(true);
  protected mapExpanded = signal(true);
  protected equipmentExpanded = signal(true);

  protected get slotList(): SlotMeta[] {
    const eq = this.game.worldContext().equipment;
    return [
      { icon: '◉', label: 'Head',       piece: eq.head },
      { icon: '▣', label: 'Torso',      piece: eq.torso },
      { icon: '▤', label: 'Legs',       piece: eq.legs },
      { icon: '▥', label: 'Feet',       piece: eq.feet },
      { icon: '◈', label: 'Right Hand', piece: eq.rightHand },
      { icon: '◇', label: 'Left Hand',  piece: eq.leftHand }
    ];
  }

  protected get totalArmor(): number {
    const player = this.game.worldContext().player;
    const eq = this.game.worldContext().equipment;
    const base = player.stats?.baseArmor ?? 0;
    const pieces = [eq.head, eq.torso, eq.legs, eq.feet, eq.rightHand, eq.leftHand];
    return base + pieces.reduce((acc, p) => acc + (p?.armor ?? 0), 0);
  }

  protected get totalStrength(): number {
    const player = this.game.worldContext().player;
    const eq = this.game.worldContext().equipment;
    const base = player.stats?.baseStrength ?? 0;
    const pieces = [eq.head, eq.torso, eq.legs, eq.feet, eq.rightHand, eq.leftHand];
    return base + pieces.reduce((acc, p) => acc + (p?.damage ?? 0), 0);
  }

  protected formatCoords(x: number, y: number): string {
    return `(${x >= 0 ? '+' : ''}${x}, ${y >= 0 ? '+' : ''}${y})`;
  }
}
