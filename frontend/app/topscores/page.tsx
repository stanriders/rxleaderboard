import { Metadata } from "next";
import { Spacer } from "@nextui-org/spacer";
import { headers } from "next/headers";

import { Score } from "@/components/score";
import { ScoreModel } from "@/api/types";
import { ApiBase } from "@/api/address";

export const metadata: Metadata = {
  title: "Top scores",
};

export default async function TopscoresPage() {
  // its uncached just to make this page build dynamically to not fail if building without backend
  const request = await fetch(`${ApiBase}/scores`, {
    cache: "no-store",
    headers: Object.fromEntries(headers()),
  }).catch((error) => console.log(`Topscores fetch failed, ${error}`));

  if (!request) return <>Error</>;

  const scores: ScoreModel[] = await request.json();

  if (!scores) return <>Error</>;

  return (
    <>
      {scores.map((row, index) => (
        <>
          <Score key={row.id} rank={index + 1} score={row} showPlayer={true} />
          <Spacer key={row.id} y={1} />
        </>
      ))}
    </>
  );
}
