
import { BeatmapModel, ScoreModel, UserModel } from "@/api/types";
import { Spacer } from "@nextui-org/spacer";
import { Card, CardBody, CardHeader } from "@nextui-org/card";
import { Image } from "@nextui-org/image";
import { Link } from "@nextui-org/link";
import {BeatmapPageTable} from "./table";
import type { Metadata } from 'next'
import { ApiBase } from "@/api/address";

type Props = {
  params: { id: number }
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const beatmap : BeatmapModel = await fetch(`${ApiBase}/beatmaps/${params.id}`).then((res) => res.json())
  return {
    title: beatmap.title
  }
}

export default async function BeatmapPage({ params }: Props) {
  const beatmap : BeatmapModel = await fetch(`${ApiBase}/beatmaps/${params.id}`, {next: {revalidate: 3600}}).then(x=> x.json())
  const scores : ScoreModel[] = await fetch(`${ApiBase}/beatmaps/${params.id}/scores`).then(x=> x.json())

  return (
  <>    
    <Card>
      <CardHeader className="absolute z-10 top-1 flex-col !items-start">
        <Card>
          <CardBody>
            <Link isExternal href={`https://osu.ppy.sh/beatmaps/${beatmap.id}`} className="text-secondary-800 text-xl">{beatmap.artist} - {beatmap.title}</Link>
            <p className="text-secondary-900 text-sm">{beatmap.difficultyName}</p>
          </CardBody>
        </Card>
      </CardHeader>
      <Image
        removeWrapper
        alt="Background"
        className="z-0 w-full h-52 object-cover"
        src={`https://assets.ppy.sh/beatmaps/${beatmap.beatmapSetId}/covers/cover@2x.jpg`}
      />
      <CardBody className="flex flex-row">
        <div className="flex flex-auto flex-col basis-1/2">
          <p className="text-default-400 text-xs">Circe Size: {beatmap.circleSize}</p>
          <p className="text-default-400 text-xs">Approach Rate: {beatmap.approachRate}</p>
          <p className="text-default-400 text-xs">Overall Difficulty: {beatmap.overallDifficulty}</p>
          <p className="text-default-400 text-xs">HP Drain: {beatmap.healthDrain}</p>
        </div>
        <div className="flex flex-col flex-auto basis-1/2">
          <p className="text-default-300 text-right">{beatmap.starRatingNormal?.toFixed(2)}*</p>
          <p className="text-3xl text-right">{beatmap.starRating?.toFixed(2)}*</p>
        </div>
      </CardBody>
    </Card>
    <Spacer y={8} />
    <BeatmapPageTable scores={scores}/>
  </>
  );
}
