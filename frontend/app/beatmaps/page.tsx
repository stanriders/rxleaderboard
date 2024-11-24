import type { Metadata } from "next";
import BeatmapListing from "./listing";
export const metadata: Metadata = {
    title: 'Beatmaps'
  };

export default async function BeatmapListingPage() {
  return (<BeatmapListing/>);
}
