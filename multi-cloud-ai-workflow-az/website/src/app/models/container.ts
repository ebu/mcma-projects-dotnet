import { Blob } from "./blob";

export interface Container {
    name: string;
    blobs: Blob[];
}