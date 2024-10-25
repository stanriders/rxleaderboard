import { Card, CardHeader, CardBody, CardFooter } from "@nextui-org/card";
import { Divider } from "@nextui-org/divider";
import { FC } from "react";
import { User } from "./user";
import { Link } from "@nextui-org/link";
import { formatDistance } from "date-fns";
import { Spacer } from "@nextui-org/spacer";
import { Mod } from "./mod";
import { BeatmapModel, ScoreModel } from "@/api/types";

type Props = { score: ScoreModel, showPlayer: boolean };

export const Score: FC<Props> = (props) => {
  const score = props.score;
  const beatmap = score.beatmap as BeatmapModel;

  return (
    <Card className="min-w-72" fullWidth key={score.id}>
      {props.showPlayer ? (
        <>
          <CardHeader className="pl-4 pt-2 pb-1">
            {score.user ? (<User user={score.user}/>) : "???"}
          </CardHeader>
          <Divider />
        </>) : 
        <></>}
      
      <CardBody className="p-2">
        <div className="flex gap-2 items-center justify-center">
          <div className="hidden md:block flex-none text-default-400 text-center w-10 text-lg">{score.grade}</div>
          <div className="flex flex-col flex-auto min-w-1">
            <div><Link href={`/beatmaps/${score.beatmapId}`} className="text-primary-500 text-sm" size="sm">{beatmap.artist} - {beatmap.title}<Spacer x={2}/><span className="text-primary-200 text-xs pr-3">{beatmap.difficultyName}</span></Link></div>
            <div className="flex-auto truncate text-default-400 text-xs">{score.totalScore} · {score.combo}x ( {score.count300} / {score.count100} / {score.count50} / {score.countMiss} )</div>
            <div className="truncate text-default-300 text-xs">{formatDistance(new Date(score.date), new Date(), { addSuffix: true })}</div>
          </div>
          
          <div className="hidden md:block flex-none text-sm">{score.mods?.map(m => <Mod key={m} mod={m}/>)}</div>
          <div className="hidden md:block flex-none text-default-500 text-center text-sm w-16">{(score.accuracy * 100)?.toFixed(2) ?? (<>- </>)}%</div>
          <div className="hidden md:block flex-none text-primary-300 items-center text-center w-24 text-lg font-semibold">{score.pp?.toFixed(0) ?? (<>- </>)}pp</div>
        </div>
      </CardBody>
      <CardFooter className="md:hidden flex gap-2 items-center justify-center">
        <div className="flex-none text-default-400 text-center w-10 text-md">{score.grade}</div>
        <div className="flex-auto text-default-500 text-right text-sm">{score.mods?.map(m => <Mod key={m} mod={m}/>)}</div>
        <div className="flex-none text-default-500 text-center text-sm">{(score.accuracy * 100)?.toFixed(2) ?? (<>- </>)}%</div>
        <div className="flex-none text-primary-300 items-center text-center w-20 text-lg font-semibold">{score.pp?.toFixed(0) ?? (<>- </>)}pp</div>
      </CardFooter>
    </Card>
  );
};
