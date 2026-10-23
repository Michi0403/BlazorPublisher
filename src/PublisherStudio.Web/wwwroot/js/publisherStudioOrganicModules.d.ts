/**
 * PublisherStudio module API: implemented by componentRuntime.js in both the editor and
 * standalone exports. 1-Wire transport/permissions belong to the PublisherStudio host,
 * not this browser-facing interface. Model output is untrusted; the host approval flow
 * remains mandatory for remote commands.
 */
export type PublisherStudioModuleMethod =
  | 'click' | 'focus' | 'blur' | 'show' | 'hide' | 'toggleVisibility'
  | 'enable' | 'disable' | 'repaint' | 'refreshData' | 'reset'
  | 'setValue' | 'setText' | 'clearFilter' | 'clearSelection'
  | 'selectAll' | 'sortByColumn' | 'scrollToTime' | 'play' | 'pause'
  | 'togglePlayback' | 'mute' | 'unmute' | 'setVolume' | 'seek';

export interface PublisherStudioModuleFunction {
  readonly name: PublisherStudioModuleMethod;
  readonly input: string;
}
export interface PublisherStudioModuleDescriptor {
  readonly elementId: string;
  readonly kind: string;
  readonly fields: readonly string[];
  readonly functions: readonly PublisherStudioModuleFunction[];
}
export interface PublisherStudioModuleCommand {
  readonly elementId: string;
  readonly method: PublisherStudioModuleMethod;
  readonly value?: unknown;
}
export interface PublisherStudioOrganicModuleRuntime {
  readonly schemaVersion: 1;
  list(root?: Element | Document): PublisherStudioModuleDescriptor[];
  describe(elementId: string): { kind: string; fields: string[]; methods: string[] } | null;
  invoke(command: PublisherStudioModuleCommand): Promise<boolean>;
}

declare global {
  interface Window {
    PublisherStudioOrganicModuleRuntime?: PublisherStudioOrganicModuleRuntime;
  }
}
