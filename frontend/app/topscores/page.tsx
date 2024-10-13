import { Metadata } from "next";
import { Score } from "@/components/score";
import { ScoreModel } from "@/api/types";
import { Spacer } from "@nextui-org/spacer";
import { ApiBase } from "@/api/address";
import { notFound } from "next/navigation";
import { error } from "console";

export const metadata: Metadata = {
  title: 'Top scores'
};

export default async function TopscoresPage() {
  // its uncached just to make this page build dynamically to not fail if building without backend
  const request = await fetch(`${ApiBase}/scores`, { cache: 'no-store' }).catch(error=> console.log(`Topscores fetch failed, ${error}`));
  if (!request)
    return <>Error</>;

  const scores : ScoreModel[] = await request.json();
  if (!scores)
    return <>Error</>;

  return (
    <>
      {scores.map((row) => (<><Score score={row} showPlayer={true} key={row.id}/><Spacer y={1} /></>))}
    </>
  );
}
