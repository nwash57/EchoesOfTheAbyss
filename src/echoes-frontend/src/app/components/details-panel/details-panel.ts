import { Component, inject, signal } from '@angular/core';
import { GameService } from '../../services/game.service';
import { MapViewerComponent } from '../map-viewer/map-viewer';
import { AccordionSectionComponent } from './accordion-section/accordion-section';
import { PlayerSectionComponent } from './player-section/player-section';
import { LocationSectionComponent } from './location-section/location-section';
import { EquipmentSectionComponent } from './equipment-section/equipment-section';
import { AdventureLogSectionComponent } from './adventure-log-section/adventure-log-section';

@Component({
  selector: 'app-details-panel',
  standalone: true,
  imports: [
    MapViewerComponent,
    AccordionSectionComponent,
    PlayerSectionComponent,
    LocationSectionComponent,
    EquipmentSectionComponent,
    AdventureLogSectionComponent
  ],
  templateUrl: './details-panel.html'
})
export class DetailsPanelComponent {
  protected game = inject(GameService);

  protected playerExpanded = signal(true);
  protected locationExpanded = signal(true);
  protected mapExpanded = signal(true);
  protected equipmentExpanded = signal(true);
  protected logExpanded = signal(true);
}
