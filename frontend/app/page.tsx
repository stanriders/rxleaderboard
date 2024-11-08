import Image from "next/image";
import { Spacer } from "@nextui-org/spacer";
import { Link } from "@nextui-org/link";

export default function Home() {
  return (
    <section className="flex flex-col items-center justify-center gap-4 py-8 md:py-10 text-xl">
      <Image src="/rv-yellowlight.svg" alt="Relaxation vault" width={256} height={256}/>
      Relaxation vault - osu!lazer relax leaderboard.
      <Spacer y={12} />
      <Link className="text-sm" isExternal href="https://discord.gg/rKyAMkmv">Join the Discord server!</Link>
    </section>
  );
}
