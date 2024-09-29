
import { Score } from "@/components/score";
import { ScoreModel } from "@/api/types";
import { Spacer } from "@nextui-org/spacer";
import { Metadata } from "next";
import { ApiBase } from "@/api/address";

export const metadata: Metadata = {
  title: 'Best scores'
};

export default async function TopscoresPage() {
  const scores : ScoreModel[] = await fetch(`${ApiBase}/scores`).then(x=> x.json())
  
  return (
  <>    
    {scores.map((row) => (<><Score score={row} showPlayer={true} key={row.id}/><Spacer y={1} /></>))}
  </>
  );
}
