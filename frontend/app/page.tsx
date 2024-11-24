import Image from "next/image";
import { Spacer } from "@nextui-org/spacer";
import { Link } from "@nextui-org/link";
import { RecentScoreTable } from "./recent-scores";
import { ScoreModel } from "@/api/types";
import { ApiBase } from "@/api/address";
import { headers } from "next/headers";

export default async function Home() {
  const request = await fetch(`${ApiBase}/scores/recent`, { cache: 'no-store', headers: Object.fromEntries(headers()) })
    .catch(error=> console.log(`Recent scores fetch failed, ${error}`));

  let scores : ScoreModel[] | null | undefined;
  if (request) {
    scores = await request.json();
  }

  return (
    <section className="flex flex-col items-center justify-center gap-1 py-6 md:py-8 px-4 text-xl">
      <Image src="/rv-yellowlight.svg" alt="Relaxation vault" width={250} height={250}/>
      <p>Relaxation vault - osu!lazer relax leaderboard.</p>
      <Spacer y={4} />
      <Link className="text-sm" isExternal href="https://discord.gg/p5zqFpBUc2">Join the Discord server!</Link>
      <Spacer y={12} />
      <RecentScoreTable scores={scores}/>
    </section>
  );
}
