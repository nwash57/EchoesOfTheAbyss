import { Component, computed, input } from '@angular/core';
import { Player, Equipment } from '../../../models/world-context.model';

@Component({
  selector: 'app-player-section',
  standalone: true,
  templateUrl: './player-section.html'
})
export class PlayerSectionComponent {
  player = input.required<Player>();
  equipment = input.required<Equipment>();
  isUpdating = input(false);

  totalArmor = computed(() => {
    const p = this.player();
    const eq = this.equipment();
    const base = p.stats?.baseArmor ?? 0;
    const pieces = [eq.head, eq.torso, eq.legs, eq.feet, eq.rightHand, eq.leftHand];
    return base + pieces.reduce((acc, piece) => acc + (piece?.armor ?? 0), 0);
  });

  totalStrength = computed(() => {
    const p = this.player();
    const eq = this.equipment();
    const base = p.stats?.baseStrength ?? 0;
    const pieces = [eq.head, eq.torso, eq.legs, eq.feet, eq.rightHand, eq.leftHand];
    return base + pieces.reduce((acc, piece) => acc + (piece?.damage ?? 0), 0);
  });

  healthPercent = computed(() => {
    const stats = this.player().stats;
    return ((stats?.currentHealth ?? 0) / (stats?.maxHealth ?? 100)) * 100;
  });

  healthColor = computed(() => {
    const health = this.player().stats?.currentHealth ?? 0;
    if (health > 50) return 'oklch(65% 0.15 145)';
    if (health > 20) return 'oklch(70% 0.15 75)';
    return 'oklch(55% 0.18 25)';
  });
}
