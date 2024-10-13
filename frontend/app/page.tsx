import { Spacer } from "@nextui-org/react";
import FAQ from "./faq";
import Image from "next/image";

export default function Home() {
  return (
    <section className="flex flex-col items-center justify-center gap-4 py-8 md:py-10 text-xl">
      <Image src="/rv-yellowlight.svg" alt="Relaxation vault" width={256} height={256}/>
      Relaxation vault - osu!lazer relax leaderboard.
      <Spacer y={12}/>
    </section>
  );
}
