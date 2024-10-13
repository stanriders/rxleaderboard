
import { Score } from "@/components/score";
import { ScoreModel, ExtendedUserModel } from "@/api/types";
import { Spacer } from "@nextui-org/spacer";
import { Avatar } from "@nextui-org/avatar";
import { Card, CardBody } from "@nextui-org/card";
import { Flag } from "@/components/flag";
import { Link } from "@nextui-org/link";
import type { Metadata } from 'next'
import { ApiBase } from "@/api/address";
import { notFound } from "next/navigation";

type Props = {
  params: { id: number }
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const player : ExtendedUserModel = await fetch(`${ApiBase}/players/${params.id}`).then((res) => res.json())
  if (!player)
    return {};

  return {
    title: player.username,
    openGraph: {
      description: `#${player.rank ?? "-"} - ${player.totalPp?.toFixed(2) ?? "-"}pp`
    },
  }
}

export default async function UserPage({ params }: Props) {
  const player : ExtendedUserModel = await fetch(`${ApiBase}/players/${params.id}`).then(x=> x.json())
  if(!player)
    return notFound();

  const scores = await fetch(`${ApiBase}/players/${params.id}/scores`).then(x=> x.json())

  return (
  <>    
    <Card>
      <CardBody className="flex flex-row items-center">
        <div className="flex flex-auto content-center truncate">
          <Avatar size="lg" src={`https://a.ppy.sh/${player.id}`} className="min-w-10 w-10 h-10 md:w-14 md:h-14"/>
          <Spacer x={2} />
          <Link className="text-primary-500 text-md md:text-2xl" size="lg" isExternal href={`https://osu.ppy.sh/users/${player.id}`}>
            <Flag country={player.countryCode} width={25}/>
            <Spacer x={1} />{player.username}
          </Link>
        </div>
        <div className="flex flex-col flex-none">
          <p className="text-sm md:text-md text-default-400 text-right">{player.totalAccuracy?.toFixed(2)}%</p>
          <p className="text-md md:text-xl text-right">{player.totalPp?.toFixed(2)}pp</p>
        </div>
        <div className="flex-none px-2 md:px-5">
          <p className="text-2xl md:text-3xl justify-center items-center font-semibold text-primary-300">#{player.rank ?? (<>-</>)}</p>
        </div>
      </CardBody>
    </Card>
    <Spacer y={10} />
    {scores.map((row: ScoreModel) => {
      row.user = player;
      return <><Score score={row} showPlayer={false} key={row.id}/><Spacer y={1} /></>})
    }
  </>
  );
}
