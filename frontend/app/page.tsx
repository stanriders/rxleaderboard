import Image from "next/image";
import { Spacer } from "@nextui-org/spacer";
import { Link } from "@nextui-org/link";
import RecentScoreTable from "./recentScores";

export default function Home() {
  return (
    <section className="flex flex-col items-center justify-center gap-1 py-6 md:py-8 px-4 text-xl">
      <Image src="/rv-yellowlight.svg" alt="Relaxation vault" width={250} height={250}/>
      Relaxation vault - osu!lazer relax leaderboard.
      <Spacer y={4} />
      <Link className="text-sm" isExternal href="https://discord.gg/p5zqFpBUc2">Join the Discord server!</Link>
      <Spacer y={12} />
      <RecentScoreTable/>
    </section>
  );
}
