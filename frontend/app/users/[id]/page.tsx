
import { Score } from "@/components/score";
import { ScoreModel, UserModel } from "@/api/types";
import { Spacer } from "@nextui-org/spacer";
import { Avatar } from "@nextui-org/avatar";
import { Card, CardBody } from "@nextui-org/card";
import { Flag } from "@/components/flag";
import { Link } from "@nextui-org/link";
import type { Metadata } from 'next'
import { ApiBase } from "@/api/address";

type Props = {
  params: { id: number }
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const player : UserModel = await fetch(`${ApiBase}/players/${params.id}`).then((res) => res.json())
  return {
    title: player.username,
  }
}

export default async function UserPage({ params }: Props) {
  const player : UserModel = await fetch(`${ApiBase}/players/${params.id}`).then(x=> x.json())
  const scores = await fetch(`${ApiBase}/players/${params.id}/scores`).then(x=> x.json())

  return (
  <>    
    <Card>
      <CardBody className="flex flex-row">
        <div className="flex flex-auto basis-1/2">
          <Avatar size="lg" src={`https://a.ppy.sh/${player.id}`}/>
          <Spacer x={2} />
          <Link className="text-secondary-700 text-2xl" size="lg" isExternal href={`https://osu.ppy.sh/users/${player.id}`}>
            <Flag country={player.countryCode} width={25}/>
            <Spacer x={1} />{player.username}
          </Link>
        </div>
        <div className="flex flex-col flex-auto basis-1/2">
          <p className="text-default-400 text-right">{player.totalAccuracy?.toFixed(2)}%</p>
          <p className="text-3xl text-right">{player.totalPp?.toFixed(2)}pp</p>
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
