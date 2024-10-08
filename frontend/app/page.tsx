import { Spacer } from "@nextui-org/react";
import FAQ from "./faq";

export default function Home() {
  return (
    <section className="flex flex-col items-center justify-center gap-4 py-8 md:py-10 text-xl">
      Relaxation vault - osu!lazer relax leaderboard.
      <Spacer y={12}/>
      <FAQ/>
    </section>
  );
}
