import { Score } from "@/components/score";
import { ScoreModel } from "@/api/types";
import { Spacer } from "@nextui-org/spacer";
import { ApiBase } from "@/api/address";
import { headers } from "next/headers";

export default async function RecentScoreTable() {
  const request = await fetch(`${ApiBase}/scores/recent`, { cache: 'no-store', headers: Object.fromEntries(headers()) })
    .catch(error=> console.log(`Recent scores fetch failed, ${error}`));

  if (!request)
    return <></>;

  const scores : ScoreModel[] = await request.json();
  if (!scores)
    return <></>;

  return (
    <div className="hidden md:block">
        <h3>Recent scores</h3>
        <Spacer y={2} />
        <div className="flex flex-col gap-2">
            {scores.map((row) => (<><Score score={row} showPlayer={true} key={row.id}/></>))}
        </div>
    </div>
  );
}
