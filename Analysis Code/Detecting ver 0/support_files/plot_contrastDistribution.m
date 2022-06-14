function  plot_contrastDistribution(dataIN, cfg)
% helper function to plot  the distribution of all target onsets,coloured
% by relative contrast. This is needed to assess whether enough
% distribution exists to examine gait effects.

% called from the script 
% plot_ReactionTime_

GFX_headY = cfg.HeadData;
% usecols = {[0 .7 0], [.7 0 0]}; % R Gr

usecols = cbrewer('qual', 'Set1',7);

figure(1); clf; set(gcf, 'color', 'w', 'units', 'normalized', 'position', [0 0 .9 .9]);
nsubs = length(cfg.subjIDs);
if strcmp(cfg.plotlevel, 'PFX')
 for ippant = 1:nsubs  
         clf;
        pc=1; % plot counter        
        pspots = [1,3,5,2,4,6]; %suplot order
        psubj= cfg.subjIDs{ippant}(1:2); % print ppid.
         % both this and the next use the same figure function:

        for nGaits_toPlot=1:2
             
            legp=[]; % for legend
            for iLR=1:3
                if nGaits_toPlot==1
                    ppantData= dataIN(ippant,iLR).gc_trgcontrIDXmatrx;                   
                    trange =  dataIN(ippant,iLR).gc_trgcontr;                   
                    strTargrange = strsplit(num2str(trange));
                    %
                    plotHead = GFX_headY(ippant).gc;
                    pidx= cfg.pidx1;
                    ftnames= {'LR', 'RL', 'combined'};
                else
                    ppantData= dataIN(ippant,iLR).doubgc_trgcontrIDXmatrx;
                    trange =  dataIN(ippant,iLR).doubgc_trgcontr;  
                    strTargrange = strsplit(num2str(trange));
                    plotHead = GFX_headY(ippant).doubgc;
                    pidx= cfg.pidx2;
                   
                    ftnames= {'LRL', 'RLR', 'combined'};
                end
            
              %x axis:          
                 timevec = 1:pidx(end);
            
                %%
                subplot(3,2,pspots(pc))
                hold on;
                yyaxis left
                allCounts=nan(1,size(ppantData,1));
                for itrg = 1:size(ppantData,1)
               % finely sampled bar, each gait "%" point.
                bh=bar(timevec, ppantData(itrg,:));
                bh.FaceColor = usecols(itrg,:); hold on;
%                 plot(timevec, ppantData(itrg,:), ['k-'])
                hold on
                allCounts(itrg) = nansum(bh.YData);
                legp(itrg)= bh;
                end
                legend([legp], {...
                    [ sprintf('%.3f',str2double(strTargrange{1})) ' (' num2str(allCounts(1)) ')'],...
                    [  sprintf('%.3f',str2double(strTargrange{2})) ' (' num2str(allCounts(2)) ')'],...
                    [ sprintf('%.3f',str2double(strTargrange{3})) ' (' num2str(allCounts(3)) ')'],...
                    [  sprintf('%.3f',str2double(strTargrange{4})) ' (' num2str(allCounts(4)) ')'],...
                    [ sprintf('%.3f',str2double(strTargrange{5})) ' (' num2str(allCounts(5)) ')'],...
                    [ sprintf('%.3f',str2double(strTargrange{6})) ' (' num2str(allCounts(6)) ')'],...
                    [ sprintf('%.3f',str2double(strTargrange{7})) ' (' num2str(allCounts(7)) ')']}, 'autoupdate', 'off');
                
                ylabel([cfg.type ' (contrast) onset [counts]']);                

                
                
                yyaxis right
                ph=plot(plotHead, ['k-o'], 'linew', 3); hold on
                set(gca,'ytick', []);
                
                title([psubj ' ' ftnames{iLR}], 'interpreter', 'none');
                midp=timevec(ceil(length(timevec)/2));
                set(gca,'fontsize', 15, 'xtick', [1, midp, timevec(end)], 'XTickLabels', {'0', '50', '100%'})
                
                xlabel([ '% of gait-cycle ']);%
                ylim([0 max(plotHead)]);
                pc=pc+1; %plotcounter
                

            end % i LR
            
        end % nGaits.
     %%  
        cd([cfg.datadir filesep  'Figures' filesep  cfg.type ' onset distribution'])
        shg
        print([psubj ' ' cfg.type ' onset distribution, split by contrast'],'-dpng');
    end % ppant


elseif strcmp(cfg.plotlevel, 'GFX')
    %% plot GFX
   
    plotcols = cbrewer('seq', 'Blues', 7);
         clf;
        pc=1; % plot counter        
        pspots = [1,3 5,2,4,6]; %suplot order
        psubj= 'GFX';
        
                            
        for nGaits_toPlot=1:2             
            legp=[]; % for legend
            ppantData=[];
            GFXhead=[];
            for iLR=1:3
                if nGaits_toPlot==1
                    %extract data all ppants.

                    for ippant = 1:size(dataIN,1)
                      ppantData(ippant,:,:) = dataIN(ippant,iLR).gc_trgcontrIDXmatrx;
                      GFXhead =  GFX_headY(ippant).gc;
                    end
                                  
                    %
                    pidx= cfg.pidx1;
                    ftnames= {'LR', 'RL', 'combined'};
                else
                     for ippant = 1:size(dataIN,1)
                      ppantData(ippant,:,:) = dataIN(ippant,iLR).doubgc_trgcontrIDXmatrx;
                      GFXhead =  GFX_headY(ippant).doubgc;
                    end
                               
                    pidx= cfg.pidx2;
                   
                    ftnames= {'LRL', 'RLR','combined'};
                end
            
              %x axis:          
                 timevec = 1:pidx(end);
            
                %%
                subplot(3,2,pspots(pc))
                hold on;
                yyaxis left
                allCounts=nan(1,size(ppantData,1));
                for itrg = 1:size(ppantData,2)
                    
                    stE = CousineauSEM(squeeze(ppantData(:,itrg,:)));
                    mP = squeeze(mean(ppantData(:,itrg,:),1));
%                sh=shadedErrorBar(timevec, mP, 0, [], 1);
               pt= plot(timevec, mP,...
                   'linestyle','-',...
                   'marker', 'none','linew',2, 'color', plotcols(itrg,:));
               
                hold on
                allt=squeeze(ppantData(:,itrg,:));
                allCounts(itrg) = nansum(allt(:));
                legp(itrg)= pt;
                end
                %% hold on;
                legend([legp], {'1','2','3','4','5','6','7'}, 'autoupdate', 'off','location', 'NorthEastOutside');                
                ylabel({[cfg.type ' onset']; ['mean counts']});   
                %% 
                %add grand mean.
                GrM = squeeze(mean(mean(ppantData,2),1));
                plot(timevec, GrM, 'k', 'linew', 3)

                
                
                yyaxis right
                ph=plot(mean(GFXhead,1), ['k-o'], 'linew', 3); hold on
                set(gca,'ytick', []);
                
                title([psubj ' ' ftnames{iLR}], 'interpreter', 'none');
                midp=timevec(ceil(length(timevec)/2));
                set(gca,'fontsize', 15, 'xtick', [1, midp, timevec(end)], 'XTickLabels', {'0', '50', '100%'})
                
                xlabel([ '% of gait-cycle ']);%
                ylim([0 max(mean(GFXhead,1))]);
                pc=pc+1; %plotcounter
                

            end % i LR
            
        end % nGaits.
     %%  
        cd([cfg.datadir filesep  'Figures' filesep  cfg.type ' onset distribution'])
        shg
        print([psubj ' ' cfg.type ' onset distribution, split by contrast'],'-dpng');
    
    
    
end