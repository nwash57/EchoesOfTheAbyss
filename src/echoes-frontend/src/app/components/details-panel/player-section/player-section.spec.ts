import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PlayerSectionComponent } from './player-section';
import { Player, Equipment, EquipmentSlot } from '../../../models/world-context.model';

describe('PlayerSectionComponent', () => {
  function createComponent(player: Player, equipment: Equipment) {
    const fixture = TestBed.createComponent(PlayerSectionComponent);
    fixture.componentRef.setInput('player', player);
    fixture.componentRef.setInput('equipment', equipment);
    fixture.componentRef.setInput('isUpdating', false);
    fixture.detectChanges();
    return fixture;
  }

  const basePlayer: Player = {
    demographics: { firstName: 'Aldric', lastName: 'Thornwood', age: 28, occupation: 'Sellsword' },
    stats: { currentHealth: 75, maxHealth: 100, baseArmor: 10, baseStrength: 15, hasUsedSecondWind: false }
  };

  const equippedGear: Equipment = {
    head: null,
    torso: { slot: EquipmentSlot.Torso, name: 'Iron Plate', armor: 20, damage: 0, description: 'Sturdy.' },
    legs: null,
    feet: null,
    rightHand: { slot: EquipmentSlot.RightHand, name: 'Steel Sword', armor: 0, damage: 25, description: 'Sharp.' },
    leftHand: null
  };

  const emptyEquipment: Equipment = {
    head: null, torso: null, legs: null, feet: null, rightHand: null, leftHand: null
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PlayerSectionComponent]
    }).compileComponents();
  });

  it('should compute totalArmor as base + equipment', () => {
    const fixture = createComponent(basePlayer, equippedGear);
    // base armor 10 + torso 20 = 30
    expect(fixture.componentInstance.totalArmor()).toBe(30);
  });

  it('should compute totalStrength as base + equipment', () => {
    const fixture = createComponent(basePlayer, equippedGear);
    // base strength 15 + rightHand 25 = 40
    expect(fixture.componentInstance.totalStrength()).toBe(40);
  });

  it('should return green health color when health > 50', () => {
    const fixture = createComponent(basePlayer, emptyEquipment);
    expect(fixture.componentInstance.healthColor()).toBe('oklch(65% 0.15 145)');
  });

  it('should return yellow health color when health between 21 and 50', () => {
    const player = { ...basePlayer, stats: { ...basePlayer.stats, currentHealth: 35 } };
    const fixture = createComponent(player, emptyEquipment);
    expect(fixture.componentInstance.healthColor()).toBe('oklch(70% 0.15 75)');
  });

  it('should return red health color when health <= 20', () => {
    const player = { ...basePlayer, stats: { ...basePlayer.stats, currentHealth: 15 } };
    const fixture = createComponent(player, emptyEquipment);
    expect(fixture.componentInstance.healthColor()).toBe('oklch(55% 0.18 25)');
  });
});
