import Image from "next/image";
import { Spacer } from "@nextui-org/spacer";
import { Link } from "@nextui-org/link";
import { headers } from "next/headers";

import { RecentScoreTable } from "./recent-scores";

import { RecentScoresResponse } from "@/api/types";
import { ApiBase } from "@/api/address";

export default async function Home() {
  const request = await fetch(`${ApiBase}/scores/recent`, {
    cache: "no-store",
    headers: Object.fromEntries(headers()),
  }).catch((error) => console.log(`Recent scores fetch failed, ${error}`));

  let response: RecentScoresResponse | null | undefined;

  if (request) {
    response = await request.json();
  }

  return (
    <section className="flex flex-col items-center justify-center gap-1 py-6 md:py-8 px-4 text-xl">
      <Image
        alt="Relaxation vault"
        height={250}
        src="/rv-yellowlight.svg"
        width={250}
      />
      <h1>Relaxation vault - osu!lazer relax leaderboard.</h1>
      <Link isExternal className="text-sm" href="https://discord.gg/p5zqFpBUc2">
        Join the Discord server!
      </Link>
      <Spacer y={10} />
      <RecentScoreTable response={response} />
    </section>
  );
}
