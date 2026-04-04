export interface ActorDemographics {
  firstName: string;
  lastName: string;
  age: number;
  occupation: string;
}

export interface PlayerStats {
  currentHealth: number;
  maxHealth: number;
  baseArmor: number;
  baseStrength: number;
}

export interface Player {
  demographics: ActorDemographics;
  stats: PlayerStats;
}

export interface Coordinates {
  x: number;
  y: number;
}

export interface Location {
  title: string;
  coordinates: Coordinates;
  shortDescription: string;
  longDescription: string;
  type?: 'landmark' | 'notable' | 'default';
}

export enum EquipmentSlot {
  Head = 1,
  Torso,
  Legs,
  Feet,
  RightHand,
  LeftHand
}

export interface EquipmentPiece {
  slot: EquipmentSlot;
  name: string;
  armor: number;
  damage: number;
  description: string;
}

export interface Equipment {
  head: EquipmentPiece | null;
  torso: EquipmentPiece | null;
  legs: EquipmentPiece | null;
  feet: EquipmentPiece | null;
  rightHand: EquipmentPiece | null;
  leftHand: EquipmentPiece | null;
}

export interface WorldContext {
  difficulty: number;
  player: Player;
  currentLocation: Location;
  equipment: Equipment;
}
