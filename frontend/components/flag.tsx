import { Image } from "@nextui-org/image";
import { FC } from "react";

type Props = { country: string | null, width: number };

export const Flag: FC<Props> = (props) => {
  function flagUrl(code: string | null) {
    if (!!code) {
      var flagName = code.split('')
        .map((c) => (c.charCodeAt(0) + 127397).toString(16))
        .join("-");

      return `https://osu.ppy.sh/assets/images/flags/${flagName}.svg`;
    }
    return "";
  }

  return (
    <Image src={flagUrl(props.country)} radius="none" className="mr-1 min-w-3" fallbackSrc={flagUrl(props.country)} width={props.width} />
  );
};
