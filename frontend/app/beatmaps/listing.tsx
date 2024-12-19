"use client";
import { Pagination } from "@nextui-org/pagination";
import { Input } from "@nextui-org/input";
import { Spacer } from "@nextui-org/spacer";
import { useEffect, useState } from "react";
import useSWR from "swr";
import { CircularProgress } from "@nextui-org/react";

import { BeatmapCard } from "./beatmap-card";

import { ApiBase } from "@/api/address";
import { BeatmapsResponse, ListingBeatmap } from "@/api/types";

const fetcher = (url: any) =>
  fetch(url)
    .then((r) => r.json())
    .catch((error) => console.log("Beatmap listing fetch error: " + error));

export default function BeatmapListing() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      setDebouncedSearch(search);
    }, 500);

    return () => clearTimeout(timeoutId);
  }, [search, 500]);

  let address = `${ApiBase}/beatmaps?page=${page}`;

  if (debouncedSearch != "") {
    address += `&search=${debouncedSearch}`;
  }
  const { data, error } = useSWR(address, fetcher);
  const response = data as BeatmapsResponse;

  if (error) return <div>Failed to load</div>;
  if (!response)
    return (
      <>
        <div className="w-full flex justify-end">
          <Input
            disabled
            classNames={{
              base: "mx-4 max-w-48 h-10",
              mainWrapper: "h-full",
              input: "text-small",
              inputWrapper:
                "h-full font-normal text-default-500 bg-default-400/20 dark:bg-default-500/20",
            }}
            placeholder="Search beatmap..."
            size="sm"
            type="search"
            value={debouncedSearch}
          />
        </div>
        <Spacer y={16} />
        <div className="flex justify-center">
          <CircularProgress aria-label="Loading..." color="primary" />
        </div>
        <Spacer y={16} />
        <div className="flex w-full justify-center">
          <Pagination
            isCompact
            isDisabled
            showControls
            showShadow
            color="secondary"
            total={0}
          />
        </div>
      </>
    );

  const pages = Math.max(1, Math.ceil((response.total ?? 0) / 50));

  return (
    <>
      <div className="w-full flex justify-end">
        <Input
          classNames={{
            base: "mx-4 max-w-48 h-10",
            mainWrapper: "h-full",
            input: "text-small",
            inputWrapper:
              "h-full font-normal text-default-500 bg-default-400/20 dark:bg-default-500/20",
          }}
          placeholder="Search beatmap..."
          size="sm"
          type="search"
          value={search}
          onValueChange={setSearch}
        />
      </div>
      <Spacer y={4} />
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {response.beatmaps?.map((row: ListingBeatmap) => (
          <BeatmapCard key={row.id} beatmap={row} />
        ))}
      </div>
      <Spacer y={4} />
      <div className="flex w-full justify-center">
        <Pagination
          isCompact
          showControls
          showShadow
          color="primary"
          page={page}
          total={pages}
          onChange={(page) => {
            setPage(page);
          }}
        />
      </div>
    </>
  );
}
