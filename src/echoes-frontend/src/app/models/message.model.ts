export enum Messager {
  Narrator = 'Narrator',
  Player = 'Player',
  System = 'System'
}

export interface Message {
  id: string;
  messager: Messager;
  speech: string;
  thoughts: string[];
  isExpanded: boolean;
}
