import { Score } from "@/components/score";
import { ScoreModel, ExtendedUserModel } from "@/api/types";
import { Spacer } from "@nextui-org/spacer";
import { Avatar } from "@nextui-org/avatar";
import { Card, CardBody, CardFooter } from "@nextui-org/card";
import { Flag } from "@/components/flag";
import { Link } from "@nextui-org/link";
import type { Metadata } from "next";
import { ApiBase } from "@/api/address";
import { notFound } from "next/navigation";
import { siteConfig } from "@/config/site";
import { headers } from "next/headers";
import { Chip } from "@nextui-org/chip";
import { PlaycountChart } from "./playcount-chart";

type Props = {
  params: { id: number };
};

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const player : ExtendedUserModel = await fetch(`${ApiBase}/players/${params.id}`, {headers: Object.fromEntries(headers())})
    .then(result => result.json())
    .catch(error => console.log("User metadata fetch error: " + error));

  if (!player)
    return {};

  return {
    title: player.username,
    openGraph: {
      siteName: siteConfig.name,
      type: "website",
      description: `#${player.rank ?? "-"} - ${player.totalPp?.toFixed(2) ?? "-"}pp`,
      images: [`https://a.ppy.sh/${player.id}`],
    },
  };
}

export default async function UserPage({ params }: Props) {
  const player : ExtendedUserModel = await fetch(`${ApiBase}/players/${params.id}`, {headers: Object.fromEntries(headers())})
    .then(result => result.json())
    .catch(error => console.log("User info fetch error: " + error));

  if(!player)
    return notFound();

  const scores = await fetch(`${ApiBase}/players/${params.id}/scores`, {headers: Object.fromEntries(headers())})
    .then(result => result.json())
    .catch(error => console.log("User scores fetch error: " + error));

  return (
    <>
      <Card className="bg-default-100 dark:bg-default-50">
        <CardBody className="flex flex-row items-center bg-content1 rounded-large">
          <div className="flex flex-auto content-center truncate">
            <Avatar size="lg" src={`https://a.ppy.sh/${player.id}`} className="min-w-10 w-10 h-10 md:w-14 md:h-14"/>
            <Spacer x={2} />
            <Link className="text-primary-500 text-md md:text-2xl" size="lg" isExternal href={`https://osu.ppy.sh/users/${player.id}`}>
              <Flag country={player.countryCode} width={25} />
              <Spacer x={1} />
              {player.username}
            </Link>
          </div>
          <div className="flex flex-col flex-none">
            <p className="text-md md:text-xl text-right">{player.totalPp?.toFixed(2)}pp</p>
            <p className="text-sm md:text-md text-default-400 text-right">{player.totalAccuracy?.toFixed(2)}%</p>
          </div>
          <div className="flex flex-col flex-none px-2 md:px-5 justify-center items-center">
            <p className="text-2xl md:text-3xl font-semibold text-primary-400">#{player.rank ?? (<>-</>)}</p>
            <p className="text-xs md:text-xs text-default-500">#{player.countryRank ?? (<>-</>)} {player.countryCode}</p>
          </div>
        </CardBody>
        <CardFooter className="pl-4 pr-5 pb-1">
          <div className="flex grow items-start self-start text-sm">Submitted scores:<Spacer x={1}/><p className="font-bold">{player.playcount}</p></div>
          <div className="flex flex-row gap-1 md:gap-3 justify-end">
            <div className="flex flex-col gap-2"><Chip size="md" radius="lg" className="border-2 border-pink-500/50 min-h-6 h-6 max-h-6 min-w-12 w-12 max-w-12">SS</Chip><p className="text-sm">{player.countSS}</p></div>
            <div className="flex flex-col gap-2"><Chip size="md" radius="lg" className="border-2 border-blue-500/50 min-h-6 h-6 max-h-6 min-w-12 w-12 max-w-12">S</Chip><p className="text-sm">{player.countS}</p></div>
            <div className="flex flex-col gap-2"><Chip size="md" radius="lg" className="border-2 border-green-500/50 min-h-6 h-6 max-h-6 min-w-12 w-12 max-w-12">A</Chip><p className="text-sm">{player.countA}</p></div>
          </div>
        </CardFooter>
      </Card>
      <Spacer y={4} />
      {scores.map((row: ScoreModel) => {
        row.user = player;
        return <><Score score={row} showPlayer={false} key={row.id}/><Spacer y={1} /></>
      })}
      <Spacer y={4} />
      <Card>
        <CardBody className="h-48">
          <PlaycountChart playcountsPerMonth={player.playcountsPerMonth}/>
        </CardBody>
      </Card>
    </>
  );
}
