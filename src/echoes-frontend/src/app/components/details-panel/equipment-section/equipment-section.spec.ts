import { TestBed } from '@angular/core/testing';
import { EquipmentSectionComponent } from './equipment-section';
import { Equipment, EquipmentSlot } from '../../../models/world-context.model';

describe('EquipmentSectionComponent', () => {
  function createComponent(equipment: Equipment) {
    const fixture = TestBed.createComponent(EquipmentSectionComponent);
    fixture.componentRef.setInput('equipment', equipment);
    fixture.componentRef.setInput('isUpdating', false);
    fixture.detectChanges();
    return fixture;
  }

  const emptyEquipment: Equipment = {
    head: null, torso: null, legs: null, feet: null, rightHand: null, leftHand: null
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EquipmentSectionComponent]
    }).compileComponents();
  });

  it('should render all 6 equipment slots', () => {
    const fixture = createComponent(emptyEquipment);
    expect(fixture.componentInstance.slotList().length).toBe(6);
  });

  it('should show empty slots', () => {
    const fixture = createComponent(emptyEquipment);
    const allEmpty = fixture.componentInstance.slotList().every(s => s.piece === null);
    expect(allEmpty).toBe(true);
  });

  it('should show equipped items', () => {
    const equipment: Equipment = {
      head: { slot: EquipmentSlot.Head, name: 'Iron Helm', armor: 15, damage: 0, description: 'Protects.' },
      torso: null,
      legs: null,
      feet: null,
      rightHand: { slot: EquipmentSlot.RightHand, name: 'Steel Sword', armor: 0, damage: 30, description: 'Sharp.' },
      leftHand: null
    };
    const fixture = createComponent(equipment);
    const slots = fixture.componentInstance.slotList();

    const head = slots.find(s => s.label === 'Head');
    expect(head?.piece?.name).toBe('Iron Helm');
    expect(head?.piece?.armor).toBe(15);

    const rightHand = slots.find(s => s.label === 'Right Hand');
    expect(rightHand?.piece?.name).toBe('Steel Sword');
    expect(rightHand?.piece?.damage).toBe(30);
  });

  it('should have correct slot labels', () => {
    const fixture = createComponent(emptyEquipment);
    const labels = fixture.componentInstance.slotList().map(s => s.label);
    expect(labels).toEqual(['Head', 'Torso', 'Legs', 'Feet', 'Right Hand', 'Left Hand']);
  });
});
