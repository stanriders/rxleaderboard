import { Card, CardHeader, CardBody, CardFooter } from "@nextui-org/card";
import { FC } from "react";
import { User } from "./user";
import { Link } from "@nextui-org/link";
import { formatDistance } from "date-fns";
import { Spacer } from "@nextui-org/spacer";
import { Mod } from "./mod";
import { BeatmapModel, ScoreModel, BeatmapStatus } from "@/api/types";
import { Tooltip } from "@nextui-org/tooltip";
import { CircularProgress } from "@nextui-org/progress";

type Props = { score: ScoreModel, showPlayer: boolean };

export const Score: FC<Props> = (props) => {
  const score = props.score;
  const beatmap = score.beatmap as BeatmapModel;
  const isCalculating = score.beatmap?.status == "Ranked" && score.pp == null;
  const opacity = score.isBest || isCalculating ? "opacity-100" : "opacity-30 hover:opacity-70";

  let pp = <Tooltip showArrow={true} content={score.pp?.toFixed(2)} delay={300}><span>{score.pp?.toFixed(0)} pp</span></Tooltip>;
  
  if (score.pp == null) {
    if (isCalculating) {
      pp = <Tooltip showArrow={true} content="Score is being calculated"><div className="flex justify-center"><CircularProgress color="primary" aria-label="Calculating..."/></div></Tooltip>
    }
    else {
      pp = <>- pp</>
    }
  }

  return (
    <Card className="min-w-72 bg-default-100 dark:bg-default-50" shadow="sm" fullWidth key={score.id}>
      {props.showPlayer ? (
        <>
          <CardHeader className="pl-5 pt-2 pb-2">
            {score.user ? (<User user={score.user}/>) : "???"}
          </CardHeader>
        </>) : 
        <></>}
      
      <CardBody className={`p-2 ${opacity} bg-content1 rounded-large`}>
        <div className="flex gap-2 items-center justify-center">
          <div className="hidden md:block flex-none text-center w-10"><Link isExternal className="text-default-400 text-lg" href={`https://osu.ppy.sh/scores/${score.id}`}>{score.grade}</Link></div>
          <div className="flex flex-col flex-auto min-w-1">
            <div><Link href={`/beatmaps/${score.beatmapId}`} className="text-primary-500 text-sm" size="sm">{beatmap.artist} - {beatmap.title}<Spacer x={2}/><span className="text-primary-200 text-xs pr-3">{beatmap.difficultyName}</span></Link></div>
            <div className="flex-auto truncate text-default-400 text-xs">{score.totalScore} · {score.combo}x ( {score.count300} / {score.count100} / {score.count50} / {score.countMiss} )</div>
            <Tooltip showArrow content={new Date(score.date).toLocaleString()}><div className="w-fit truncate text-default-400 text-xs">{formatDistance(new Date(score.date), new Date(), { addSuffix: true })}</div></Tooltip>
          </div>
          
          <div className="hidden md:block flex-none text-sm">{score.mods?.map(m => <Mod key={m} mod={m}/>)}</div>
          <div className="hidden md:block flex-none text-default-500 text-center text-sm w-16">{(score.accuracy * 100)?.toFixed(2) ?? (<>- </>)}%</div>
          <div className="hidden md:block flex-none text-primary-300 items-center text-center w-24 text-lg font-semibold">{pp}</div>
        </div>
      </CardBody>
      <CardFooter className={`md:hidden flex gap-2 items-center justify-center ${opacity}`}>
        <div className="flex-none text-center w-10"><Link isExternal className="text-default-400 text-md" href={`https://osu.ppy.sh/scores/${score.id}`}>{score.grade}</Link></div>
        <div className="flex-auto text-default-500 text-right text-sm">{score.mods?.map(m => <Mod key={m} mod={m}/>)}</div>
        <div className="flex-none text-default-500 text-center text-sm">{(score.accuracy * 100)?.toFixed(2) ?? (<>- </>)}%</div>
        <div className="flex-none text-primary-300 items-center text-center w-20 text-lg font-semibold">{pp}</div>
      </CardFooter>
    </Card>
  );
};
