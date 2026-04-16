import { Component, computed, input } from '@angular/core';
import { Equipment, EquipmentPiece } from '../../../models/world-context.model';

interface SlotMeta {
  icon: string;
  label: string;
  piece: EquipmentPiece | null;
}

@Component({
  selector: 'app-equipment-section',
  standalone: true,
  templateUrl: './equipment-section.html'
})
export class EquipmentSectionComponent {
  equipment = input.required<Equipment>();
  isUpdating = input(false);

  slotList = computed<SlotMeta[]>(() => {
    const eq = this.equipment();
    return [
      { icon: '◉', label: 'Head',       piece: eq.head },
      { icon: '▣', label: 'Torso',      piece: eq.torso },
      { icon: '▤', label: 'Legs',       piece: eq.legs },
      { icon: '▥', label: 'Feet',       piece: eq.feet },
      { icon: '◈', label: 'Right Hand', piece: eq.rightHand },
      { icon: '◇', label: 'Left Hand',  piece: eq.leftHand }
    ];
  });
}
