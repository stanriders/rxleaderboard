import { Card, CardHeader, CardBody } from "@nextui-org/card";
import { FC } from "react";
import { Link } from "@nextui-org/link";
import { Image } from "@nextui-org/image";

import { ListingBeatmap } from "@/api/types";

type Props = { beatmap: ListingBeatmap };

export const BeatmapCard: FC<Props> = (props) => {
  const beatmap = props.beatmap;

  return (
    <Link href={`/beatmaps/${beatmap.id}`}>
      <Card key={beatmap.id} fullWidth isPressable>
        <CardHeader className="absolute z-10 flex-row !items-start h-32 dark:bg-gradient-to-t dark:from-black/5 dark:to-black/60">
          <Card isBlurred className="w-full">
            <CardBody>
              <p className="text-primary-300 text-xs">{beatmap.artist}</p>
              <p className="text-primary-500 text-md">{beatmap.title}</p>
            </CardBody>
          </Card>
        </CardHeader>
        <Image
          removeWrapper
          alt="Background"
          className="z-0 w-full h-32 object-cover"
          src={`https://assets.ppy.sh/beatmaps/${beatmap.beatmapSetId}/covers/cover.jpg`}
        />
        <CardBody className="p-2 flex flex-row text-sm">
          <div className="flex grow flex-col overflow-hidden">
            <p className="text-primary-400 truncate">
              {beatmap.difficultyName}
            </p>
            <p className="text-default-600">{beatmap.status}</p>
          </div>

          <div className="flex grow flex-col justify-right text-right">
            <p className="text-primary-400">
              {beatmap.starRating?.toFixed(2)}*
            </p>
            <p className="text-default-600">{beatmap.playcount} scores</p>
          </div>
        </CardBody>
      </Card>
    </Link>
  );
};
